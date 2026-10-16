namespace Clinica.Domain.Comum;

/// <summary>
/// Base de toda entidade: o que a torna entidade é ter identidade (Id).
/// </summary>
public abstract class Entidade
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
