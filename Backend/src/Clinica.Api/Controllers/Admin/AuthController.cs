using Clinica.Api.Configuracao;
using Clinica.Application.Autenticacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Clinica.Api.Controllers.Admin;

public sealed record LoginDto(string Login, string Senha);
public sealed record PrimeiroAcessoDto(string Codigo, string CodigoDeAcesso, string NovaSenha);
public sealed record TrocarSenhaDto(string SenhaAtual, string NovaSenha);

[ApiController]
[Route("api/auth")]
public class AuthController(Autenticacao autenticacao) : ControllerBase
{
    /// <summary>Login com identificador (@a123456@) ou e-mail. Devolve o token JWT.</summary>
    [HttpPost("login")]
    [EnableRateLimiting(RegistroDaAplicacao.LimiteDeLogin)]
    public Task<SessaoDto> Login(LoginDto dados, CancellationToken ct) =>
        autenticacao.EntrarAsync(dados.Login, dados.Senha, ct);

    /// <summary>Primeiro acesso: identificador + código recebido + senha nova.</summary>
    [HttpPost("primeiro-acesso")]
    [EnableRateLimiting(RegistroDaAplicacao.LimiteDeLogin)]
    public Task<SessaoDto> PrimeiroAcesso(PrimeiroAcessoDto dados, CancellationToken ct) =>
        autenticacao.PrimeiroAcessoAsync(dados.Codigo, dados.CodigoDeAcesso, dados.NovaSenha, ct);

    [Authorize]
    [HttpGet("eu")]
    public Task<UsuarioLogadoDto> Eu(CancellationToken ct) => autenticacao.ObterUsuarioLogadoAsync(ct);

    /// <summary>Troca a própria senha. Devolve um token novo (o anterior deixa de valer).</summary>
    [Authorize]
    [HttpPost("senha")]
    public Task<SessaoDto> TrocarSenha(TrocarSenhaDto dados, CancellationToken ct) =>
        autenticacao.TrocarSenhaAsync(dados.SenhaAtual, dados.NovaSenha, ct);
}
