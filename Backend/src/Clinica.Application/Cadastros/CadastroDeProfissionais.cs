using Clinica.Application.Abstracoes;
using Clinica.Application.Auditoria;
using Clinica.Application.Comum;
using Clinica.Domain.Comum;
using Clinica.Domain.Entities;
using Clinica.Domain.ValueObjects;

namespace Clinica.Application.Cadastros;

/// <summary>Horário de trabalho como texto ("09:00"), que é o que o formulário envia.</summary>
public sealed record HorarioDeTrabalhoDto(DayOfWeek Dia, string Inicio, string Fim);

public sealed record ProfissionalDto(Guid Id, string Nome, bool Ativa, IReadOnlyList<Guid> ServicoIds, IReadOnlyList<HorarioDeTrabalhoDto> Horarios);

public sealed record SalvarProfissionalDto(string Nome, bool Ativa, IReadOnlyList<Guid> ServicoIds, IReadOnlyList<HorarioDeTrabalhoDto> Horarios);

public class CadastroDeProfissionais(
    IProfissionalRepositorio profissionais,
    IServicoRepositorio servicos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    Historico historico)
{
    public async Task<IReadOnlyList<ProfissionalDto>> ListarAsync(CancellationToken ct) =>
        (await profissionais.ListarAsync(ct)).OrderBy(p => p.Nome).Select(ParaDto).ToList();

    public async Task<ProfissionalDto> ObterAsync(Guid id, CancellationToken ct) =>
        ParaDto(await ObterEntidadeAsync(id, ct));

    public async Task<ProfissionalDto> CriarAsync(SalvarProfissionalDto dados, CancellationToken ct)
    {
        await GarantirServicosExistemAsync(dados.ServicoIds, ct);
        var profissional = new Profissional(dados.Nome, dados.ServicoIds, LerHorarios(dados.Horarios));
        profissional.DefinirAtiva(dados.Ativa);
        profissionais.Adicionar(profissional);
        await historico.RegistrarAsync("profissional.criar", $"Cadastrou a profissional {profissional.Nome} ({await ResumoAsync(profissional, ct)}).", profissional.Id, ct);
        await unidadeDeTrabalho.SalvarAsync(ct);
        return ParaDto(profissional);
    }

    public async Task<ProfissionalDto> AtualizarAsync(Guid id, SalvarProfissionalDto dados, CancellationToken ct)
    {
        await GarantirServicosExistemAsync(dados.ServicoIds, ct);
        var profissional = await ObterEntidadeAsync(id, ct);
        var antes = $"{profissional.Nome} ({await ResumoAsync(profissional, ct)})";
        profissional.Atualizar(dados.Nome, dados.ServicoIds, LerHorarios(dados.Horarios));
        profissional.DefinirAtiva(dados.Ativa);
        await historico.RegistrarAsync("profissional.alterar",
            $"Alterou a profissional. Antes: {antes}. Depois: {profissional.Nome} ({await ResumoAsync(profissional, ct)}).", profissional.Id, ct);
        await unidadeDeTrabalho.SalvarAsync(ct);
        return ParaDto(profissional);
    }

    private async Task GarantirServicosExistemAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var existentes = (await servicos.ListarAsync(ct)).Select(s => s.Id).ToHashSet();
        if (ids.Any(id => !existentes.Contains(id)))
            throw new RegraDeNegocioException("Um dos procedimentos selecionados não existe.");
    }

    private async Task<string> ResumoAsync(Profissional p, CancellationToken ct)
    {
        var nomes = (await servicos.ListarAsync(ct)).Where(s => p.Realiza(s.Id)).Select(s => s.Nome).OrderBy(n => n);
        var dias = p.Horarios.GroupBy(h => h.Dia).Select(g => $"{Dia(g.Key)} {string.Join(" e ", g.Select(h => $"{Horas.Escrever(h.Inicio)}-{Horas.Escrever(h.Fim)}"))}");
        return $"ativa: {Textos.SimNao(p.Ativa)}; procedimentos: {string.Join(", ", nomes).NullSeVazio() ?? "nenhum"}; horários: {string.Join(", ", dias).NullSeVazio() ?? "nenhum"}";
    }

    private static string Dia(DayOfWeek dia) =>
        dia switch
        {
            DayOfWeek.Monday => "seg", DayOfWeek.Tuesday => "ter", DayOfWeek.Wednesday => "qua", DayOfWeek.Thursday => "qui",
            DayOfWeek.Friday => "sex", DayOfWeek.Saturday => "sáb", _ => "dom"
        };

    private async Task<Profissional> ObterEntidadeAsync(Guid id, CancellationToken ct) =>
        await profissionais.ObterAsync(id, ct) ?? throw new NaoEncontradoException("Profissional não encontrada.");

    private static IEnumerable<HorarioDeTrabalho> LerHorarios(IEnumerable<HorarioDeTrabalhoDto> horarios) =>
        horarios.Select(h => new HorarioDeTrabalho(h.Dia, Horas.Ler(h.Inicio, "Início"), Horas.Ler(h.Fim, "Fim"))).ToList();

    private static ProfissionalDto ParaDto(Profissional p) =>
        new(p.Id, p.Nome, p.Ativa, p.ServicoIds.ToList(),
            p.Horarios.Select(h => new HorarioDeTrabalhoDto(h.Dia, Horas.Escrever(h.Inicio), Horas.Escrever(h.Fim))).ToList());
}
