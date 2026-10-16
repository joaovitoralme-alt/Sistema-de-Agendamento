using System.Globalization;

namespace Clinica.Application.Comum;

/// <summary>Formatos usados nas frases do histórico.</summary>
internal static class Textos
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string DataHora(DateTime valor) => valor.ToString("dd/MM/yyyy HH:mm", PtBr);

    public static string SimNao(bool valor) => valor ? "sim" : "não";
}

internal static class ExtensoesDeTexto
{
    public static string? NullSeVazio(this string texto) => string.IsNullOrEmpty(texto) ? null : texto;
}
