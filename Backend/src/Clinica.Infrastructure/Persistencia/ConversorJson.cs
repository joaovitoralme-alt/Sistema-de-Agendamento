using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Clinica.Infrastructure.Persistencia;

/// <summary>
/// Guarda listas (fotos, horários, dias fechados...) numa coluna JSON.
/// O comparador ensina o EF a perceber quando a lista mudou.
/// </summary>
internal static class ConversorJson
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);

    public static ValueConverter<T, string> Conversor<T>() where T : new() =>
        new(valor => Serializar(valor), json => Desserializar<T>(json));

    public static ValueComparer<T> Comparador<T>() where T : new() =>
        new(
            (a, b) => Serializar(a) == Serializar(b),
            valor => Serializar(valor).GetHashCode(),
            valor => Desserializar<T>(Serializar(valor)));

    private static string Serializar<T>(T? valor) => JsonSerializer.Serialize(valor, Opcoes);

    private static T Desserializar<T>(string json) where T : new() =>
        JsonSerializer.Deserialize<T>(json, Opcoes) ?? new T();
}
