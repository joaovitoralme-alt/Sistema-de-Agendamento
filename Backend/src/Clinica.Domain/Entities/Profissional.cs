using Clinica.Domain.Comum;
using Clinica.Domain.ValueObjects;

namespace Clinica.Domain.Entities;

/// <summary>Quem realiza os atendimentos. Cada uma tem seus procedimentos e horários.</summary>
public class Profissional : Entidade
{
    public string Nome { get; private set; } = string.Empty;
    public bool Ativa { get; private set; } = true;

    /// <summary>Ids dos serviços que ela realiza (relacionamento muitos-para-muitos).</summary>
    public List<Guid> ServicoIds { get; private set; } = [];

    public List<HorarioDeTrabalho> Horarios { get; private set; } = [];

    private Profissional() { }

    public Profissional(string nome, IEnumerable<Guid> servicoIds, IEnumerable<HorarioDeTrabalho> horarios)
    {
        Atualizar(nome, servicoIds, horarios);
    }

    public void Atualizar(string nome, IEnumerable<Guid> servicoIds, IEnumerable<HorarioDeTrabalho> horarios)
    {
        var listaDeHorarios = horarios.ToList();

        // Dois intervalos no mesmo dia não podem se cruzar (ex.: 09h-12h e 11h-14h).
        for (var i = 0; i < listaDeHorarios.Count; i++)
            for (var j = i + 1; j < listaDeHorarios.Count; j++)
                if (listaDeHorarios[i].SeSobrepoe(listaDeHorarios[j]))
                    throw new RegraDeNegocioException("Existem horários de trabalho que se sobrepõem no mesmo dia.");

        Nome = Validar.Texto(nome, "Nome", 100);
        ServicoIds = servicoIds.Distinct().ToList();
        Horarios = listaDeHorarios.OrderBy(h => h.Dia).ThenBy(h => h.Inicio).ToList();
    }

    public void DefinirAtiva(bool ativa) => Ativa = ativa;

    public bool Realiza(Guid servicoId) => ServicoIds.Contains(servicoId);

    public IEnumerable<HorarioDeTrabalho> HorariosNoDia(DayOfWeek dia) =>
        Horarios.Where(h => h.Dia == dia);
}
