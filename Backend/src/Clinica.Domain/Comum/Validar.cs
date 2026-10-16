namespace Clinica.Domain.Comum;

/// <summary>
/// Pequenos ajudantes para as validações que se repetem nas entidades.
/// </summary>
internal static class Validar
{
    public static string Texto(string? valor, string campo, int maximo, bool obrigatorio = true)
    {
        var limpo = (valor ?? string.Empty).Trim();

        if (obrigatorio && limpo.Length == 0)
            throw new RegraDeNegocioException($"{campo} é obrigatório.");

        if (limpo.Length > maximo)
            throw new RegraDeNegocioException($"{campo} pode ter no máximo {maximo} caracteres.");

        return limpo;
    }

    public static string? TextoOpcional(string? valor, string campo, int maximo)
    {
        var limpo = Texto(valor, campo, maximo, obrigatorio: false);
        return limpo.Length == 0 ? null : limpo;
    }

    /// <summary>
    /// Todas as datas do sistema são o "horário de parede" da clínica.
    /// Guardamos sem fuso (Kind = Unspecified) para o banco não tentar converter.
    /// </summary>
    public static DateTime HorarioLocal(DateTime valor) =>
        DateTime.SpecifyKind(valor, DateTimeKind.Unspecified);
}
