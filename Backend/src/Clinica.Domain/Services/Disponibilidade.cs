using Clinica.Domain.Entities;
using Clinica.Domain.ValueObjects;

namespace Clinica.Domain.Services;

public enum MotivoIndisponibilidade
{
    ClinicaFechada,
    ProfissionalInativa,
    NaoRealizaServico,
    ForaDoExpediente,
    Ausencia,
    HorarioOcupado
}

public sealed record VerificacaoDeDisponibilidade(bool Disponivel, MotivoIndisponibilidade? Motivo, string? Detalhe)
{
    public static readonly VerificacaoDeDisponibilidade Livre = new(true, null, null);

    public static VerificacaoDeDisponibilidade Indisponivel(MotivoIndisponibilidade motivo, string detalhe) =>
        new(false, motivo, detalhe);
}

/// <summary>
/// Serviço de domínio: responde "essa profissional pode atender nesse período?".
/// É C# puro: recebe tudo pronto e não acessa banco, por isso é fácil de testar.
/// Criar, remarcar, trocar profissional e listar alertas usam esta MESMA regra.
/// </summary>
public static class Disponibilidade
{
    private static readonly TimeSpan Grade = TimeSpan.FromMinutes(HorarioDeTrabalho.GradeEmMinutos);

    public static VerificacaoDeDisponibilidade Verificar(
        Profissional profissional,
        Guid servicoId,
        Periodo periodo,
        ConfiguracaoClinica configuracao,
        IEnumerable<Agendamento> agendamentos,
        IEnumerable<Ausencia> ausencias,
        Guid? ignorarAgendamentoId = null)
    {
        // 1. A clínica abre nesse dia da semana?
        if (configuracao.FechadaEm(periodo.Inicio.DayOfWeek))
            return VerificacaoDeDisponibilidade.Indisponivel(MotivoIndisponibilidade.ClinicaFechada,
                "A clínica não abre neste dia da semana.");

        if (!profissional.Ativa)
            return VerificacaoDeDisponibilidade.Indisponivel(MotivoIndisponibilidade.ProfissionalInativa,
                $"{profissional.Nome} está inativa.");

        if (!profissional.Realiza(servicoId))
            return VerificacaoDeDisponibilidade.Indisponivel(MotivoIndisponibilidade.NaoRealizaServico,
                $"{profissional.Nome} não realiza este procedimento.");

        // 2. O período inteiro (com a folga) cabe dentro de um intervalo de trabalho dela?
        var data = DateOnly.FromDateTime(periodo.Inicio);
        var dentroDoExpediente = profissional
            .HorariosNoDia(periodo.Inicio.DayOfWeek)
            .Any(h => h.NaData(data).Contem(periodo));

        if (!dentroDoExpediente)
            return VerificacaoDeDisponibilidade.Indisponivel(MotivoIndisponibilidade.ForaDoExpediente,
                $"Fora do horário de trabalho de {profissional.Nome}.");

        // 3. Ausência dela ou da clínica toda?
        var ausencia = ausencias.FirstOrDefault(a => a.Afeta(profissional.Id) && a.Periodo.SeSobrepoe(periodo));
        if (ausencia is not null)
            return VerificacaoDeDisponibilidade.Indisponivel(MotivoIndisponibilidade.Ausencia,
                ausencia.ValeParaClinicaToda
                    ? $"Clínica fechada ({DescreverMotivo(ausencia)})."
                    : $"{profissional.Nome} está ausente ({DescreverMotivo(ausencia)}).");

        // 4. Choca com outro agendamento ativo dela? (Pendente também bloqueia.)
        var ocupado = agendamentos.Any(a =>
            a.Ativo &&
            a.ProfissionalId == profissional.Id &&
            a.Id != ignorarAgendamentoId &&
            a.PeriodoBloqueado.SeSobrepoe(periodo));

        if (ocupado)
            return VerificacaoDeDisponibilidade.Indisponivel(MotivoIndisponibilidade.HorarioOcupado,
                $"{profissional.Nome} já tem um atendimento neste horário.");

        return VerificacaoDeDisponibilidade.Livre;
    }

    /// <summary>
    /// Todos os inícios livres (de 15 em 15 min) de uma profissional, num dia, para um serviço.
    /// </summary>
    public static IReadOnlyList<DateTime> HorariosLivres(
        Profissional profissional,
        Servico servico,
        DateOnly data,
        ConfiguracaoClinica configuracao,
        IReadOnlyCollection<Agendamento> agendamentos,
        IReadOnlyCollection<Ausencia> ausencias)
    {
        var livres = new List<DateTime>();

        foreach (var expediente in profissional.HorariosNoDia(data.DayOfWeek))
        {
            var intervalo = expediente.NaData(data);

            for (var inicio = intervalo.Inicio; inicio + servico.TempoBloqueado <= intervalo.Fim; inicio += Grade)
            {
                var periodo = new Periodo(inicio, inicio + servico.TempoBloqueado);
                if (Verificar(profissional, servico.Id, periodo, configuracao, agendamentos, ausencias).Disponivel)
                    livres.Add(inicio);
            }
        }

        return livres;
    }

    private static string DescreverMotivo(Ausencia ausencia) =>
        ausencia.Descricao ?? ausencia.Motivo switch
        {
            Enums.MotivoAusencia.Ferias => "férias",
            Enums.MotivoAusencia.Feriado => "feriado",
            _ => "ausência"
        };
}
