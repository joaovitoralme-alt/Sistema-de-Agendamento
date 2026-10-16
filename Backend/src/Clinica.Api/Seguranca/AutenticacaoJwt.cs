using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Clinica.Application.Abstracoes;
using Clinica.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Clinica.Api.Seguranca;

/// <summary>Configuração do JWT, lida da seção "Jwt" do appsettings.</summary>
public sealed class OpcoesJwt
{
    public string Emissor { get; set; } = "Clinica";
    public string Audiencia { get; set; } = "Clinica.Painel";
    public int ExpiracaoEmHoras { get; set; } = 10;

    /// <summary>Segredo que assina os tokens. Pelo menos 32 caracteres. Em produção vem de variável de ambiente.</summary>
    public string Chave { get; set; } = string.Empty;

    public SymmetricSecurityKey ChaveDeAssinatura() => new(Encoding.UTF8.GetBytes(Chave));
}

/// <summary>
/// Cria o token (JWT) depois do login. O token é como um crachá assinado: diz quem é a pessoa
/// e até quando vale. A API confere a assinatura em cada requisição, sem precisar de sessão no servidor.
/// </summary>
public class GeradorDeTokenJwt(OpcoesJwt opcoes) : IGeradorDeToken
{
    public TokenDeAcesso Gerar(Usuario usuario)
    {
        var expiraEm = DateTime.UtcNow.AddHours(opcoes.ExpiracaoEmHoras);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = opcoes.Emissor,
            Audience = opcoes.Audiencia,
            Expires = expiraEm,
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimsDoUsuario.Id, usuario.Id.ToString()),
                new Claim(ClaimsDoUsuario.Nome, usuario.Nome),
                new Claim(ClaimsDoUsuario.Codigo, usuario.Codigo),
                new Claim(ClaimsDoUsuario.Selo, usuario.SeloDeSeguranca.ToString())
            ]),
            SigningCredentials = new SigningCredentials(opcoes.ChaveDeAssinatura(), SecurityAlgorithms.HmacSha256)
        };

        return new TokenDeAcesso(new JsonWebTokenHandler().CreateToken(descritor), expiraEm);
    }
}

public static class AutenticacaoJwt
{
    public static IServiceCollection AddAutenticacaoJwt(this IServiceCollection services, IConfiguration configuration, IHostEnvironment ambiente)
    {
        var opcoes = configuration.GetSection("Jwt").Get<OpcoesJwt>() ?? new OpcoesJwt();

        if (string.IsNullOrWhiteSpace(opcoes.Chave))
        {
            if (!ambiente.IsDevelopment())
                throw new InvalidOperationException("Configure Jwt:Chave (variável de ambiente Jwt__Chave) com pelo menos 32 caracteres.");

            // Só em desenvolvimento: chave aleatória a cada execução (os logins caem ao reiniciar a API).
            opcoes.Chave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        }

        if (opcoes.Chave.Length < 32)
            throw new InvalidOperationException("Jwt:Chave precisa ter pelo menos 32 caracteres.");

        services.AddSingleton(opcoes);
        services.AddSingleton<IGeradorDeToken, GeradorDeTokenJwt>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false; // mantém os nomes curtos ("sub", "name"...)
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opcoes.Emissor,
                    ValidateAudience = true,
                    ValidAudience = opcoes.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = opcoes.ChaveDeAssinatura(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = ClaimsDoUsuario.Nome
                };

                // Além da assinatura, confere no banco se o usuário continua ativo
                // e se a senha não mudou desde que o token foi emitido.
                o.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async contexto =>
                    {
                        if (!await ValidacaoDeSessao.ValidaAsync(contexto.Principal, contexto.HttpContext))
                            contexto.Fail("Sessão encerrada.");
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
