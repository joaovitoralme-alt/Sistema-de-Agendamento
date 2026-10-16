using Clinica.Application.Abstracoes;
using Clinica.Domain.Entities;
using Clinica.Domain.Services;
using Clinica.Domain.ValueObjects;

namespace Clinica.Application.Agendamentos;

/// <summary>Tudo o que a regra de disponibilidade precisa, carregado de uma vez.</summary>
public sealed record ContextoDaAgenda(
    ConfiguracaoClinica Configuracao,
    List<Servico> Servicos,
    List<Profissional> Profissionais,
    List<Agendamento> Agendamentos,
    List<Ausencia> Ausencias)
{
    public Servico? Servico(Guid id) => Servicos.FirstOrDefault(s => s.Id == id);
    public Profissional? Profissional(Guid id) => Profissionais.FirstOrDefault(p => p.Id == id);

    public VerificacaoDeDisponibilidade Verificar(Profissional profissional, Guid servicoId, Periodo periodo, Guid? ignorar = null) =>
        Disponibilidade.Verificar(profissional, servicoId, periodo, Configuracao, Agendamentos, Ausencias, ignorar);

    public AgendamentoDto ParaDto(Agendamento a)
    {
        var servico = Servico(a.ServicoId);
        var fimDoAtendimento = a.FimBloqueado - TimeSpan.FromMinutes(Domain.Entities.Servico.FolgaEmMinutos);

        return new AgendamentoDto(
            a.Id, a.Inicio, fimDoAtendimento, a.FimBloqueado,
            a.ServicoId, servico?.Nome ?? "(serviço removido)",
            a.ProfissionalId, Profissional(a.ProfissionalId)?.Nome ?? "(profissional removida)",
            a.ClienteNome, a.ClienteTelefone, a.Observacao, a.Status, a.CriadoEm);
    }
}

public class LeitorDaAgenda(
    IConfiguracaoRepositorio configuracoes,
    IServicoRepositorio servicos,
    IProfissionalRepositorio profissionais,
    IAgendamentoRepositorio agendamentos,
    IAusenciaRepositorio ausencias)
{
    public async Task<ContextoDaAgenda> CarregarAsync(DateTime de, DateTime ate, CancellationToken ct) =>
        new(
            await configuracoes.ObterAsync(ct),
            await servicos.ListarAsync(ct),
            await profissionais.ListarAsync(ct),
            await agendamentos.ListarAsync(de, ate, ct),
            await ausencias.ListarAsync(de, ate, ct));

    public Task<ContextoDaAgenda> CarregarDiaAsync(DateOnly dia, CancellationToken ct) =>
        CarregarAsync(dia.ToDateTime(TimeOnly.MinValue), dia.AddDays(1).ToDateTime(TimeOnly.MinValue), ct);
}
