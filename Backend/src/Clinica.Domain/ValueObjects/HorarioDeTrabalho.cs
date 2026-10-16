using Clinica.Domain.Comum;

namespace Clinica.Domain.ValueObjects;

/// <summary>
/// Um intervalo de trabalho num dia da semana. Ex.: segunda, 09:00 às 12:00.
/// Objeto de valor: não tem Id, é imutável e dois horários com os mesmos dados são iguais.
/// </summary>
public sealed record HorarioDeTrabalho
{
    public const int GradeEmMinutos = 15;

    // DayOfWeek já vem com o .NET (Sunday, Monday...). Usar o dele evita converter
    // na hora de comparar com DateTime.DayOfWeek.
    public DayOfWeek Dia { get; }
    public TimeOnly Inicio { get; }
    public TimeOnly Fim { get; }

    // Propriedades só com "get" não podem ser alteradas depois de criadas,
    // nem por uma expressão "with". Então todo HorarioDeTrabalho passa por esta validação.
    public HorarioDeTrabalho(DayOfWeek dia, TimeOnly inicio, TimeOnly fim)
    {
        if (inicio >= fim)
            throw new RegraDeNegocioException("O início do expediente precisa ser antes do fim.");

        if (!EstaNaGrade(inicio) || !EstaNaGrade(fim))
            throw new RegraDeNegocioException($"Início e fim do expediente precisam ser múltiplos de {GradeEmMinutos} minutos.");

        Dia = dia;
        Inicio = inicio;
        Fim = fim;
    }

    public TimeSpan Duracao => Fim - Inicio;

    /// <summary>Converte este horário num período concreto, numa data específica.</summary>
    public Periodo NaData(DateOnly data) =>
        new(data.ToDateTime(Inicio), data.ToDateTime(Fim));

    public bool SeSobrepoe(HorarioDeTrabalho outro) =>
        Dia == outro.Dia && Inicio < outro.Fim && outro.Inicio < Fim;

    private static bool EstaNaGrade(TimeOnly hora) =>
        hora.Minute % GradeEmMinutos == 0 && hora.Second == 0 && hora.Millisecond == 0;
}
