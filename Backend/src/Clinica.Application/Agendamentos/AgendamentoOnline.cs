using Clinica.Application.Abstracoes;
using Clinica.Application.Comum;
using Clinica.Domain.Comum;
using Clinica.Domain.Entities;
using Clinica.Domain.Services;
using Clinica.Domain.ValueObjects;

namespace Clinica.Application.Agendamentos;

/// <summary>
/// Casos de uso do CLIENTE no site: ver horários livres, ver profissionais livres e agendar.
/// </summary>
public class AgendamentoOnline(
    LeitorDaAgenda leitor,
    IAgendamentoRepositorio agendamentos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IRelogio relogio)
{
    public const string HorarioIndisponivel = "Este horário não está mais disponível. Por favor, escolha outro.";

    public async Task<IReadOnlyList<TimeOnly>> ListarHorariosDisponiveisAsync(Guid servicoId, DateOnly data, CancellationToken ct)
    {
        var agenda = await leitor.CarregarDiaAsync(data, ct);
        var servico = ServicoAtivo(agenda, servicoId);

        if (!DataDentroDaJanela(agenda.Configuracao, data))
            return [];

        var limite = InicioMaisCedoPermitido(agenda.Configuracao);

        return agenda.Profissionais
            .Where(p => p.Ativa && p.Realiza(servico.Id))
            .SelectMany(p => Disponibilidade.HorariosLivres(p, servico, data, agenda.Configuracao, agenda.Agendamentos, agenda.Ausencias))
            .Where(inicio => inicio >= limite)
            .Select(TimeOnly.FromDateTime)
            .Distinct()
            .Order()
            .ToList();
    }

    public async Task<IReadOnlyList<ProfissionalResumoDto>> ListarProfissionaisDisponiveisAsync(Guid servicoId, DateTime inicio, CancellationToken ct)
    {
        inicio = Horas.Local(inicio);
        var agenda = await leitor.CarregarDiaAsync(DateOnly.FromDateTime(inicio), ct);

        if (!agenda.Configuracao.PermitirEscolherProfissional)
            throw new RegraDeNegocioException("A escolha de profissional está desativada.");

        var servico = ServicoAtivo(agenda, servicoId);
        var periodo = new Periodo(inicio, inicio + servico.TempoBloqueado);

        return agenda.Profissionais
            .Where(p => agenda.Verificar(p, servico.Id, periodo).Disponivel)
            .OrderBy(p => p.Nome)
            .Select(p => new ProfissionalResumoDto(p.Id, p.Nome))
            .ToList();
    }

    public Task<AgendamentoCriadoDto> CriarAsync(CriarAgendamentoDto dados, CancellationToken ct) =>
        // Dentro da trava: verificamos DE NOVO se o horário está livre, porque outra pessoa
        // pode ter pegado o horário entre a tela mostrar e o cliente confirmar.
        TravaDaAgenda.ExecutarAsync(async () =>
        {
            var inicio = Horas.Local(dados.Inicio);
            var agenda = await leitor.CarregarDiaAsync(DateOnly.FromDateTime(inicio), ct);
            var configuracao = agenda.Configuracao;
            var servico = ServicoAtivo(agenda, dados.ServicoId);

            if (inicio < InicioMaisCedoPermitido(configuracao) || !DataDentroDaJanela(configuracao, DateOnly.FromDateTime(inicio)))
                throw new RegraDeNegocioException("Este horário não está aberto para agendamento.");

            var periodo = new Periodo(inicio, inicio + servico.TempoBloqueado);
            Profissional profissional;

            if (dados.ProfissionalId is { } escolhida)
            {
                if (!configuracao.PermitirEscolherProfissional)
                    throw new RegraDeNegocioException("A escolha de profissional está desativada.");

                profissional = agenda.Profissional(escolhida)
                    ?? throw new NaoEncontradoException("Profissional não encontrada.");

                if (!agenda.Verificar(profissional, servico.Id, periodo).Disponivel)
                    throw new RegraDeNegocioException(HorarioIndisponivel);
            }
            else
            {
                // Sem escolha: entre as livres, a que tem menos atendimentos no dia.
                profissional = agenda.Profissionais
                    .Where(p => agenda.Verificar(p, servico.Id, periodo).Disponivel)
                    .OrderBy(p => agenda.Agendamentos.Count(a => a.Ativo && a.ProfissionalId == p.Id))
                    .ThenBy(p => p.Nome)
                    .FirstOrDefault()
                    ?? throw new RegraDeNegocioException(HorarioIndisponivel);
            }

            var agendamento = new Agendamento(servico, profissional.Id, inicio,
                dados.ClienteNome, dados.ClienteTelefone, dados.Observacao, relogio.Agora);

            agendamentos.Adicionar(agendamento);
            await unidadeDeTrabalho.SalvarAsync(ct);

            return new AgendamentoCriadoDto(agendamento.Id, agendamento.Inicio, servico.Nome,
                configuracao.PermitirEscolherProfissional ? profissional.Nome : null);
        }, ct);

    private static Servico ServicoAtivo(ContextoDaAgenda agenda, Guid servicoId)
    {
        var servico = agenda.Servico(servicoId);
        if (servico is null || !servico.Ativo)
            throw new NaoEncontradoException("Procedimento não encontrado.");
        return servico;
    }

    private DateTime InicioMaisCedoPermitido(ConfiguracaoClinica configuracao) =>
        relogio.Agora.AddMinutes(configuracao.AntecedenciaMinimaEmMinutos);

    private bool DataDentroDaJanela(ConfiguracaoClinica configuracao, DateOnly data)
    {
        var hoje = DateOnly.FromDateTime(relogio.Agora);
        return data >= hoje && data <= hoje.AddDays(configuracao.DiasMaximosParaAgendar);
    }
}
