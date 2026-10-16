using Clinica.Domain.Comum;
using Clinica.Domain.Enums;
using Clinica.Domain.ValueObjects;

namespace Clinica.Domain.Entities;

/// <summary>
/// Férias, feriado ou saída pontual. Sem profissional = vale para a clínica toda.
/// </summary>
public class Ausencia : Entidade
{
    public Guid? ProfissionalId { get; private set; }
    public DateTime Inicio { get; private set; }
    public DateTime Fim { get; private set; }
    public MotivoAusencia Motivo { get; private set; }
    public string? Descricao { get; private set; }

    private Ausencia() { }

    public Ausencia(Guid? profissionalId, DateTime inicio, DateTime fim, MotivoAusencia motivo, string? descricao)
    {
        if (!Enum.IsDefined(motivo))
            throw new RegraDeNegocioException("Motivo inválido.");

        var periodo = new Periodo(inicio, fim); // valida início antes do fim
        ProfissionalId = profissionalId;
        Inicio = periodo.Inicio;
        Fim = periodo.Fim;
        Motivo = motivo;
        Descricao = Validar.TextoOpcional(descricao, "Descrição", 200);
    }

    public Periodo Periodo => new(Inicio, Fim);

    public bool ValeParaClinicaToda => ProfissionalId is null;

    public bool Afeta(Guid profissionalId) => ProfissionalId is null || ProfissionalId == profissionalId;
}
