namespace Clinica.Domain.Comum;

/// <summary>
/// Lançada quando alguém tenta fazer algo que quebra uma regra do negócio
/// (ex.: agendar num horário ocupado). A API transforma isso numa resposta 400
/// com a mensagem para o usuário.
/// </summary>
public class RegraDeNegocioException(string mensagem) : Exception(mensagem);
