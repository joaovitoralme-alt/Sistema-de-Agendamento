using Clinica.Domain.Comum;

namespace Clinica.Domain.ValueObjects;

/// <summary>
/// Um trecho de tempo com início e fim. É aqui que mora a regra de sobreposição,
/// usada para agendamentos e ausências.
/// </summary>
public sealed record Periodo
{
    public DateTime Inicio { get; }
    public DateTime Fim { get; }

    public Periodo(DateTime inicio, DateTime fim)
    {
        if (fim <= inicio)
            throw new RegraDeNegocioException("O fim precisa ser depois do início.");

        Inicio = Validar.HorarioLocal(inicio);
        Fim = Validar.HorarioLocal(fim);
    }

    /// <summary>
    /// Dois períodos se chocam quando um começa antes do outro terminar, nos dois sentidos.
    /// Encostar (um termina às 15h e o outro começa às 15h) NÃO é choque.
    /// </summary>
    public bool SeSobrepoe(Periodo outro) => Inicio < outro.Fim && outro.Inicio < Fim;

    public bool Contem(Periodo outro) => Inicio <= outro.Inicio && outro.Fim <= Fim;
}
