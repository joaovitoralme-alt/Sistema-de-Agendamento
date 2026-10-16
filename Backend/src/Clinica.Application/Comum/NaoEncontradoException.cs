namespace Clinica.Application.Comum;

/// <summary>A API transforma isso numa resposta 404.</summary>
public class NaoEncontradoException(string mensagem) : Exception(mensagem);
