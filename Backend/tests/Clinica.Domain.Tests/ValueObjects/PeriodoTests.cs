using Clinica.Domain.ValueObjects;

namespace Clinica.Domain.Tests.ValueObjects;

/// <summary>Os 6 casos de sobreposição que montamos na modelagem.</summary>
public class PeriodoTests
{
    private static Periodo P(int h1, int m1, int h2, int m2) =>
        new(new DateTime(2026, 10, 5, h1, m1, 0), new DateTime(2026, 10, 5, h2, m2, 0));

    [Theory]
    [InlineData(14, 0, 15, 0, 14, 30, 15, 30, true)]   // B começa no meio de A
    [InlineData(14, 0, 15, 0, 15, 0, 16, 0, false)]    // um termina quando o outro começa
    [InlineData(14, 0, 15, 0, 13, 0, 14, 0, false)]    // mesma coisa, pelo outro lado
    [InlineData(14, 0, 16, 0, 14, 30, 15, 0, true)]    // B inteiro dentro de A
    [InlineData(14, 30, 15, 0, 14, 0, 16, 0, true)]    // A inteiro dentro de B
    [InlineData(9, 0, 10, 0, 14, 0, 15, 0, false)]     // longe um do outro
    public void Sobreposicao(int a1, int a2, int a3, int a4, int b1, int b2, int b3, int b4, bool esperado)
    {
        var a = P(a1, a2, a3, a4);
        var b = P(b1, b2, b3, b4);

        Assert.Equal(esperado, a.SeSobrepoe(b));
        Assert.Equal(esperado, b.SeSobrepoe(a)); // a regra vale nos dois sentidos
    }
}
