namespace Clinica.Application.Comum;

/// <summary>Login ou senha errados, sessão inválida. A API transforma isso numa resposta 401.</summary>
public class NaoAutorizadoException(string mensagem) : Exception(mensagem);
