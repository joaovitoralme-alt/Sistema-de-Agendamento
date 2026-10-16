using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using Clinica.Domain.Services;
using Clinica.Domain.ValueObjects;

namespace Clinica.Domain.Tests.Services;

public class DisponibilidadeTests
{
    // Segunda-feira, 5 de outubro de 2026.
    private static readonly DateOnly Segunda = new(2026, 10, 5);

    private readonly Servico _drenagem = new("Drenagem", "", 60);   // bloqueia 75 min
    private readonly Profissional _ana;
    private readonly ConfiguracaoClinica _configuracao = ConfiguracaoClinica.Padrao();

    public DisponibilidadeTests()
    {
        _ana = new Profissional("Ana", [_drenagem.Id],
        [
            new HorarioDeTrabalho(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            new HorarioDeTrabalho(DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(18, 0))
        ]);
    }

    private static DateTime Em(int hora, int minuto = 0) => Segunda.ToDateTime(new TimeOnly(hora, minuto));

    private IReadOnlyList<DateTime> Livres(List<Agendamento>? agendamentos = null, List<Ausencia>? ausencias = null) =>
        Disponibilidade.HorariosLivres(_ana, _drenagem, Segunda, _configuracao, agendamentos ?? [], ausencias ?? []);

    private Agendamento AgendarAs(int hora) =>
        new(_drenagem, _ana.Id, Em(hora), "Cliente", "11988887777", null, Em(8));

    [Fact]
    public void Ultimo_horario_da_manha_e_o_que_ainda_cabe_com_a_folga()
    {
        var livres = Livres();

        Assert.Equal(Em(9), livres[0]);
        Assert.Contains(Em(10, 45), livres);       // 10:45 + 75 min = 12:00, cabe
        Assert.DoesNotContain(Em(11), livres);     // 11:00 + 75 min = 12:15, passa do expediente
        Assert.Equal(Em(16, 45), livres[^1]);      // 16:45 + 75 min = 18:00
    }

    [Fact]
    public void Agendamento_pendente_bloqueia_o_horario()
    {
        var livres = Livres([AgendarAs(14)]);      // ocupa 14:00 até 15:15

        Assert.DoesNotContain(Em(13), livres);     // 13:00 + 75 = 14:15, choca
        Assert.Contains(Em(15, 15), livres);       // encosta no fim: pode
        Assert.DoesNotContain(Em(15), livres);
    }

    [Fact]
    public void Agendamento_cancelado_libera_o_horario()
    {
        var cancelado = AgendarAs(14);
        cancelado.Cancelar();

        Assert.Contains(Em(14), Livres([cancelado]));
    }

    [Fact]
    public void Ferias_da_profissional_bloqueiam_o_dia()
    {
        var ferias = new Ausencia(_ana.Id, Em(0), Em(23, 45), MotivoAusencia.Ferias, null);

        Assert.Empty(Livres(ausencias: [ferias]));
    }

    [Fact]
    public void Feriado_da_clinica_toda_bloqueia_todas()
    {
        var feriado = new Ausencia(null, Em(0), Em(23, 45), MotivoAusencia.Feriado, null);

        Assert.Empty(Livres(ausencias: [feriado]));
    }

    [Fact]
    public void Dia_fechado_da_clinica_nao_tem_horarios()
    {
        _configuracao.AtualizarRegrasDeAgenda(true, [DayOfWeek.Monday], 60, 60);

        Assert.Empty(Livres());
    }

    [Fact]
    public void Profissional_que_nao_faz_o_servico_nao_esta_disponivel()
    {
        var massagem = new Servico("Massagem", "", 60);
        var periodo = new Periodo(Em(9), Em(10, 15));

        var resultado = Disponibilidade.Verificar(_ana, massagem.Id, periodo, _configuracao, [], []);

        Assert.False(resultado.Disponivel);
        Assert.Equal(MotivoIndisponibilidade.NaoRealizaServico, resultado.Motivo);
    }

    [Fact]
    public void Ao_verificar_um_agendamento_existente_ele_nao_conflita_consigo_mesmo()
    {
        var agendamento = AgendarAs(14);

        var resultado = Disponibilidade.Verificar(_ana, _drenagem.Id, agendamento.PeriodoBloqueado,
            _configuracao, [agendamento], [], ignorarAgendamentoId: agendamento.Id);

        Assert.True(resultado.Disponivel);
    }
}
