using Clinica.Domain.Comum;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;

namespace Clinica.Domain.Tests.Entities;

public class AgendamentoTests
{
    private readonly Servico _limpeza = new("Limpeza de Pele", "", 45);
    private static readonly DateTime Inicio = new(2026, 10, 5, 14, 0, 0);

    private Agendamento Novo(string telefone = "(11) 98888-7777") =>
        new(_limpeza, Guid.NewGuid(), Inicio, "Maria", telefone, null, Inicio.AddDays(-1));

    [Fact]
    public void Limpeza_de_45_minutos_bloqueia_60()
    {
        Assert.Equal(Inicio.AddMinutes(60), Novo().FimBloqueado);
    }

    [Fact]
    public void Nasce_pendente_e_pode_ser_confirmado()
    {
        var agendamento = Novo();
        Assert.Equal(StatusAgendamento.Pendente, agendamento.Status);

        agendamento.Confirmar();
        Assert.Equal(StatusAgendamento.Confirmado, agendamento.Status);
    }

    [Fact]
    public void Cancelado_nao_pode_ser_remarcado()
    {
        var agendamento = Novo();
        agendamento.Cancelar();

        Assert.Throws<RegraDeNegocioException>(() => agendamento.Remarcar(Inicio.AddDays(1), _limpeza.TempoBloqueado));
    }

    [Theory]
    [InlineData("(11) 98888-7777", "11988887777")]
    [InlineData("+55 11 3333-4444", "1133334444")]
    public void Telefone_fica_so_com_numeros(string digitado, string guardado)
    {
        Assert.Equal(guardado, Novo(digitado).ClienteTelefone);
    }

    [Fact]
    public void Telefone_sem_ddd_e_recusado()
    {
        Assert.Throws<RegraDeNegocioException>(() => Novo("98888-7777"));
    }

    [Fact]
    public void Horario_fora_da_grade_e_recusado()
    {
        Assert.Throws<RegraDeNegocioException>(() =>
            new Agendamento(_limpeza, Guid.NewGuid(), Inicio.AddMinutes(10), "Maria", "11988887777", null, Inicio));
    }
}
