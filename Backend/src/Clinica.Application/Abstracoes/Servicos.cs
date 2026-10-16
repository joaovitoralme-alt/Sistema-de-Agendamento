namespace Clinica.Application.Abstracoes;

/// <summary>
/// Dá a hora atual NO FUSO DA CLÍNICA. Ninguém usa DateTime.Now direto:
/// assim os testes conseguem "congelar" o relógio.
/// </summary>
public interface IRelogio
{
    DateTime Agora { get; }
}

public interface IArmazenamentoDeFotos
{
    /// <summary>Salva a imagem e devolve o caminho público (ex.: /fotos/abc.jpg).</summary>
    Task<string> SalvarAsync(Stream conteudo, string extensao, CancellationToken ct);

    Task RemoverAsync(string caminhoPublico, CancellationToken ct);
}

public interface IHashDeSenha
{
    string Gerar(string senha);
    bool Verificar(string hash, string senha);
}

/// <summary>Quem está logado nesta requisição. A Api preenche a partir do token JWT.</summary>
public interface IUsuarioAtual
{
    Guid? Id { get; }
}

public sealed record TokenDeAcesso(string Token, DateTime ExpiraEmUtc);

/// <summary>Cria o token de acesso (JWT). Implementado na Api, que conhece a chave de assinatura.</summary>
public interface IGeradorDeToken
{
    TokenDeAcesso Gerar(Domain.Entities.Usuario usuario);
}
