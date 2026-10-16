using System.Globalization;
using Clinica.Domain.Comum;

namespace Clinica.Application.Comum;

internal static class Horas
{
    private static readonly string[] Formatos = ["HH:mm", "HH:mm:ss", "H:mm"];

    public static TimeOnly Ler(string? valor, string campo)
    {
        if (TimeOnly.TryParseExact(valor?.Trim(), Formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
            return hora;

        throw new RegraDeNegocioException($"{campo}: horário inválido ({valor}). Use o formato HH:mm.");
    }

    public static string Escrever(TimeOnly hora) => hora.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static DateTime Local(DateTime valor) => DateTime.SpecifyKind(valor, DateTimeKind.Unspecified);
}
