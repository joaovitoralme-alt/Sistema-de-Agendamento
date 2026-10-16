using System.Security.Claims;
using Clinica.Application.Abstracoes;
using Clinica.Application.Autenticacao;

namespace Clinica.Api.Seguranca;

/// <summary>Nomes das informações (claims) que vão dentro do token.</summary>
public static class ClaimsDoUsuario
{
    public const string Id = "sub";
    public const string Nome = "name";
    public const string Codigo = "codigo";
    public const string Selo = "selo";
}

/// <summary>Lê do token quem está fazendo a requisição atual.</summary>
public class UsuarioAtual(IHttpContextAccessor http) : IUsuarioAtual
{
    public Guid? Id =>
        Guid.TryParse(http.HttpContext?.User.FindFirstValue(ClaimsDoUsuario.Id), out var id) ? id : null;
}

public static class ValidacaoDeSessao
{
    public static async Task<bool> ValidaAsync(ClaimsPrincipal? usuario, HttpContext http)
    {
        if (!Guid.TryParse(usuario?.FindFirstValue(ClaimsDoUsuario.Id), out var id)
            || !Guid.TryParse(usuario?.FindFirstValue(ClaimsDoUsuario.Selo), out var selo))
            return false;

        var autenticacao = http.RequestServices.GetRequiredService<Autenticacao>();
        return await autenticacao.SessaoValidaAsync(id, selo, http.RequestAborted);
    }
}
