using Clinica.Application.Abstracoes;
using Clinica.Application.Auditoria;
using Clinica.Application.Comum;
using Clinica.Domain.Comum;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using Clinica.Domain.ValueObjects;

namespace Clinica.Application.Agendamentos;

/// <summary>
/// Casos de uso do ADMINISTRADOR sobre agendamentos: listar, confirmar, cancelar,
/// remarcar, trocar profissional e ver os alertas de conflito.
/// </summary>
public class GestaoDeAgendamentos(
    LeitorDaAgenda leitor,
    IAgendamentoRepositorio agendamentos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IRelogio relogio,
    Historico historico)
{
    public async Task<IReadOnlyList<AgendamentoDto>> ListarAsync(DateTime de, DateTime ate,
        StatusAgendamento? status, Guid? profissionalId, CancellationToken ct)
    {
        var agenda = await leitor.CarregarAsync(Horas.Local(de), Horas.Local(ate), ct);

        return agenda.Agendamentos
            .Where(a => a.Inicio >= de && a.Inicio < ate)
            .Where(a => status is null || a.Status == status)
            .Where(a => profissionalId is null || a.ProfissionalId == profissionalId)
            .OrderBy(a => a.Inicio)
            .Select(agenda.ParaDto)
            .ToList();
    }

    public async Task ConfirmarAsync(Guid id, CancellationToken ct)
    {
        var agendamento = await ObterAsync(id, ct);
        agendamento.Confirmar();
        await historico.RegistrarAsync("agendamento.confirmar", $"Confirmou o agendamento de {await DescreverAsync(agendamento, ct)}.", agendamento.Id, ct);
        await unidadeDeTrabalho.SalvarAsync(ct);
    }

    public async Task CancelarAsync(Guid id, CancellationToken ct)
    {
        var agendamento = await ObterAsync(id, ct);
        agendamento.Cancelar();
        await historico.RegistrarAsync("agendamento.cancelar", $"Cancelou o agendamento de {await DescreverAsync(agendamento, ct)}.", agendamento.Id, ct);
        await unidadeDeTrabalho.SalvarAsync(ct);
    }

    public Task<bool> RemarcarAsync(Guid id, DateTime novoInicio, CancellationToken ct) =>
        TravaDaAgenda.ExecutarAsync(async () =>
        {
            novoInicio = Horas.Local(novoInicio);
            if (novoInicio < relogio.Agora)
                throw new RegraDeNegocioException("Não é possível remarcar para o passado.");

            var agendamento = await ObterAsync(id, ct);
            var agenda = await leitor.CarregarDiaAsync(DateOnly.FromDateTime(novoInicio), ct);
            var servico = agenda.Servico(agendamento.ServicoId)
                ?? throw new NaoEncontradoException("Procedimento não encontrado.");
            var profissional = ProfissionalDe(agenda, agendamento.ProfissionalId);

            var periodo = new Periodo(novoInicio, novoInicio + servico.TempoBloqueado);
            GarantirDisponivel(agenda, profissional, servico.Id, periodo, agendamento.Id);

            var inicioAnterior = agendamento.Inicio;
            agendamento.Remarcar(novoInicio, servico.TempoBloqueado);
            await historico.RegistrarAsync("agendamento.remarcar",
                $"Remarcou o agendamento de {agendamento.ClienteNome} ({servico.Nome} com {profissional.Nome}) de {Textos.DataHora(inicioAnterior)} para {Textos.DataHora(novoInicio)}.",
                agendamento.Id, ct);
            await unidadeDeTrabalho.SalvarAsync(ct);
            return true;
        }, ct);

    public Task<bool> TrocarProfissionalAsync(Guid id, Guid profissionalId, CancellationToken ct) =>
        TravaDaAgenda.ExecutarAsync(async () =>
        {
            var agendamento = await ObterAsync(id, ct);
            var agenda = await leitor.CarregarDiaAsync(DateOnly.FromDateTime(agendamento.Inicio), ct);
            var profissional = ProfissionalDe(agenda, profissionalId);

            GarantirDisponivel(agenda, profissional, agendamento.ServicoId, agendamento.PeriodoBloqueado, agendamento.Id);

            var anterior = agenda.Profissional(agendamento.ProfissionalId)?.Nome ?? "(removida)";
            agendamento.TrocarProfissional(profissional.Id);
            await historico.RegistrarAsync("agendamento.trocar_profissional",
                $"Trocou a profissional do agendamento de {agendamento.ClienteNome} ({agenda.Servico(agendamento.ServicoId)?.Nome}, {Textos.DataHora(agendamento.Inicio)}) de {anterior} para {profissional.Nome}.",
                agendamento.Id, ct);
            await unidadeDeTrabalho.SalvarAsync(ct);
            return true;
        }, ct);

    /// <summary>Quem poderia assumir este agendamento (para a tela de troca de profissional).</summary>
    public async Task<IReadOnlyList<ProfissionalResumoDto>> ProfissionaisParaTrocaAsync(Guid id, CancellationToken ct)
    {
        var agendamento = await ObterAsync(id, ct);
        var agenda = await leitor.CarregarDiaAsync(DateOnly.FromDateTime(agendamento.Inicio), ct);

        return agenda.Profissionais
            .Where(p => p.Id != agendamento.ProfissionalId)
            .Where(p => agenda.Verificar(p, agendamento.ServicoId, agendamento.PeriodoBloqueado, agendamento.Id).Disponivel)
            .OrderBy(p => p.Nome)
            .Select(p => new ProfissionalResumoDto(p.Id, p.Nome))
            .ToList();
    }

    /// <summary>
    /// Os alertas são CALCULADOS: um agendamento futuro está em conflito quando a profissional
    /// não estaria disponível para ele se ele não existisse. Nada é guardado no banco.
    /// </summary>
    public async Task<IReadOnlyList<AlertaDto>> ListarAlertasAsync(CancellationToken ct)
    {
        var agora = relogio.Agora;
        var agenda = await leitor.CarregarAsync(agora, agora.AddYears(2), ct);

        var alertas = new List<AlertaDto>();
        foreach (var agendamento in agenda.Agendamentos.Where(a => a.Ativo && a.Inicio >= agora).OrderBy(a => a.Inicio))
        {
            var profissional = agenda.Profissional(agendamento.ProfissionalId);
            string? motivo = profissional is null
                ? "A profissional deste agendamento não existe mais."
                : agenda.Verificar(profissional, agendamento.ServicoId, agendamento.PeriodoBloqueado, agendamento.Id) is { Disponivel: false } v
                    ? v.Detalhe
                    : null;

            if (motivo is not null)
                alertas.Add(new AlertaDto(agenda.ParaDto(agendamento), motivo));
        }

        return alertas;
    }

    /// <summary>"Maria (Limpeza de Pele com Ana, 05/10/2026 14:00)" — usado nas frases do histórico.</summary>
    private async Task<string> DescreverAsync(Agendamento agendamento, CancellationToken ct)
    {
        var agenda = await leitor.CarregarDiaAsync(DateOnly.FromDateTime(agendamento.Inicio), ct);
        var dto = agenda.ParaDto(agendamento);
        return $"{dto.ClienteNome} ({dto.ServicoNome} com {dto.ProfissionalNome}, {Textos.DataHora(dto.Inicio)})";
    }

    private async Task<Agendamento> ObterAsync(Guid id, CancellationToken ct) =>
        await agendamentos.ObterAsync(id, ct) ?? throw new NaoEncontradoException("Agendamento não encontrado.");

    private static Profissional ProfissionalDe(ContextoDaAgenda agenda, Guid id) =>
        agenda.Profissional(id) ?? throw new NaoEncontradoException("Profissional não encontrada.");

    private static void GarantirDisponivel(ContextoDaAgenda agenda, Profissional profissional, Guid servicoId, Periodo periodo, Guid ignorar)
    {
        var verificacao = agenda.Verificar(profissional, servicoId, periodo, ignorar);
        if (!verificacao.Disponivel)
            throw new RegraDeNegocioException(verificacao.Detalhe ?? "Horário indisponível.");
    }
}
