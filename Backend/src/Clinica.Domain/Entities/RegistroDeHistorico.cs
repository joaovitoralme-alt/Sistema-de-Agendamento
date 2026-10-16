using Clinica.Domain.Comum;

namespace Clinica.Domain.Entities;

/// <summary>
/// Uma linha do histórico: quem fez o quê e quando. Nunca é alterada nem apagada.
/// Guardamos o nome e o identificador do usuário na própria linha, para o histórico
/// continuar legível mesmo se o cadastro da pessoa mudar depois.
/// </summary>
public class RegistroDeHistorico : Entidade
{
    public DateTime DataHora { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string UsuarioCodigo { get; private set; } = string.Empty;
    public string UsuarioNome { get; private set; } = string.Empty;

    /// <summary>Código da ação, bom para filtrar (ex.: "agendamento.confirmar").</summary>
    public string Acao { get; private set; } = string.Empty;

    /// <summary>Frase para pessoas lerem (ex.: "Confirmou o agendamento de Maria...").</summary>
    public string Descricao { get; private set; } = string.Empty;

    public Guid? AlvoId { get; private set; }

    private RegistroDeHistorico() { }

    public RegistroDeHistorico(DateTime dataHora, Usuario? usuario, string acao, string descricao, Guid? alvoId = null)
    {
        DataHora = Validar.HorarioLocal(dataHora);
        UsuarioId = usuario?.Id;
        UsuarioCodigo = usuario?.Codigo ?? "-";
        UsuarioNome = usuario?.Nome ?? "Sistema";
        Acao = Validar.Texto(acao, "Ação", 60);
        Descricao = Validar.Texto(descricao, "Descrição", 1000);
        AlvoId = alvoId;
    }
}
