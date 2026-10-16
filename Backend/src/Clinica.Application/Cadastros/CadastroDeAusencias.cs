using Clinica.Application.Abstracoes;
using Clinica.Application.Agendamentos;
using Clinica.Application.Auditoria;
using Clinica.Application.Comum;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;

namespace Clinica.Application.Cadastros;

public sealed record AusenciaDto(Guid Id, Guid? ProfissionalId, string ProfissionalNome, DateTime Inicio, DateTime Fim, MotivoAusencia Motivo, string? Descricao);

public sealed record SalvarAusenciaDto(Guid? ProfissionalId, DateTime Inicio, DateTime Fim, MotivoAusencia Motivo, string? Descricao);

/// <summary>Resultado de cadastrar uma ausência: ela é salva, e os conflitos voltam como aviso.</summary>
public sealed record AusenciaCriadaDto(AusenciaDto Ausencia, IReadOnlyList<AlertaDto> Conflitos);

public class CadastroDeAusencias(
    IAusenciaRepositorio ausencias,
    IProfissionalRepositorio profissionais,
    GestaoDeAgendamentos gestao,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IRelogio relogio,
    Historico historico)
{
    public async Task<IReadOnlyList<AusenciaDto>> ListarAsync(DateTime? de, DateTime? ate, CancellationToken ct)
    {
        var inicio = Horas.Local(de ?? relogio.Agora.Date);
        var fim = Horas.Local(ate ?? inicio.AddYears(1));
        var nomes = await NomesAsync(ct);

        return (await ausencias.ListarAsync(inicio, fim, ct))
            .OrderBy(a => a.Inicio)
            .Select(a => ParaDto(a, nomes))
            .ToList();
    }

    public async Task<AusenciaCriadaDto> CriarAsync(SalvarAusenciaDto dados, CancellationToken ct)
    {
        var nomes = await NomesAsync(ct);
        if (dados.ProfissionalId is { } id && !nomes.ContainsKey(id))
            throw new NaoEncontradoException("Profissional não encontrada.");

        var ausencia = new Ausencia(dados.ProfissionalId, Horas.Local(dados.Inicio), Horas.Local(dados.Fim), dados.Motivo, dados.Descricao);
        ausencias.Adicionar(ausencia);
        await historico.RegistrarAsync("ausencia.criar", $"Cadastrou ausência: {Descrever(ausencia, nomes)}.", ausencia.Id, ct);
        await unidadeDeTrabalho.SalvarAsync(ct);

        // Salva normalmente e devolve, como aviso, os agendamentos que ficaram em conflito.
        var conflitos = (await gestao.ListarAlertasAsync(ct))
            .Where(alerta => ausencia.Afeta(alerta.Agendamento.ProfissionalId)
                             && alerta.Agendamento.Inicio < ausencia.Fim
                             && ausencia.Inicio < alerta.Agendamento.FimBloqueado)
            .ToList();

        return new AusenciaCriadaDto(ParaDto(ausencia, nomes), conflitos);
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct)
    {
        var ausencia = await ausencias.ObterAsync(id, ct) ?? throw new NaoEncontradoException("Ausência não encontrada.");
        ausencias.Remover(ausencia);
        await historico.RegistrarAsync("ausencia.remover", $"Removeu ausência: {Descrever(ausencia, await NomesAsync(ct))}.", ausencia.Id, ct);
        await unidadeDeTrabalho.SalvarAsync(ct);
    }

    private async Task<Dictionary<Guid, string>> NomesAsync(CancellationToken ct) =>
        (await profissionais.ListarAsync(ct)).ToDictionary(p => p.Id, p => p.Nome);

    private static string Descrever(Ausencia a, Dictionary<Guid, string> nomes)
    {
        var dto = ParaDto(a, nomes);
        var motivo = a.Motivo switch { MotivoAusencia.Ferias => "férias", MotivoAusencia.Feriado => "feriado", _ => "outro motivo" };
        return $"{dto.ProfissionalNome}, {motivo}{(a.Descricao is null ? "" : $" ({a.Descricao})")}, de {Textos.DataHora(a.Inicio)} até {Textos.DataHora(a.Fim)}";
    }

    private static AusenciaDto ParaDto(Ausencia a, Dictionary<Guid, string> nomes) =>
        new(a.Id, a.ProfissionalId,
            a.ProfissionalId is { } id ? nomes.GetValueOrDefault(id, "(removida)") : "Clínica toda",
            a.Inicio, a.Fim, a.Motivo, a.Descricao);
}
