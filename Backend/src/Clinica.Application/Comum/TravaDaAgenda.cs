namespace Clinica.Application.Comum;

/// <summary>
/// Evita que dois clientes peguem o mesmo horário ao mesmo tempo: só uma operação
/// que mexe na agenda roda por vez. Suficiente para uma clínica com um servidor só.
/// Se um dia rodar em vários servidores, troque por uma trava no banco de dados.
/// </summary>
public static class TravaDaAgenda
{
    private static readonly SemaphoreSlim Semaforo = new(1, 1);

    public static async Task<T> ExecutarAsync<T>(Func<Task<T>> operacao, CancellationToken ct)
    {
        await Semaforo.WaitAsync(ct);
        try
        {
            return await operacao();
        }
        finally
        {
            Semaforo.Release();
        }
    }
}
