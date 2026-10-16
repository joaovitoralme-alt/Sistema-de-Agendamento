using Clinica.Domain.Comum;
using Clinica.Domain.ValueObjects;

namespace Clinica.Domain.Tests.ValueObjects;

public class HorarioDeTrabalhoTests
{
    [Fact]
    public void Cria_horario_valido()
    {
        var horario = new HorarioDeTrabalho(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));

        Assert.Equal(TimeSpan.FromHours(3), horario.Duracao);
    }

    [Fact]
    public void Dois_horarios_com_os_mesmos_dados_sao_iguais()
    {
        var a = new HorarioDeTrabalho(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));
        var b = new HorarioDeTrabalho(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));

        Assert.Equal(a, b); // é isso que faz dele um objeto de valor
    }

    [Theory]
    [InlineData(12, 0, 9, 0)]   // fim antes do início
    [InlineData(9, 0, 9, 0)]    // início igual ao fim
    [InlineData(9, 10, 12, 0)]  // fora da grade de 15 minutos
    public void Recusa_horario_invalido(int hIni, int mIni, int hFim, int mFim)
    {
        Assert.Throws<RegraDeNegocioException>(() =>
            new HorarioDeTrabalho(DayOfWeek.Monday, new TimeOnly(hIni, mIni), new TimeOnly(hFim, mFim)));
    }
}
