using System.Threading.RateLimiting;
using Clinica.Api.Seguranca;
using Clinica.Application.Abstracoes;
using Clinica.Application.Agendamentos;
using Clinica.Application.Auditoria;
using Clinica.Application.Autenticacao;
using Clinica.Application.Cadastros;
using Clinica.Application.Usuarios;

namespace Clinica.Api.Configuracao;

public static class RegistroDaAplicacao
{
    public const string LimiteDeAgendamento = "agendamento";
    public const string LimiteDeLogin = "login";

    /// <summary>Registra os casos de uso da camada Application.</summary>
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        services.AddScoped<LeitorDaAgenda>();
        services.AddScoped<AgendamentoOnline>();
        services.AddScoped<GestaoDeAgendamentos>();
        services.AddScoped<CadastroDeServicos>();
        services.AddScoped<CadastroDeProfissionais>();
        services.AddScoped<CadastroDeAusencias>();
        services.AddScoped<Configuracoes>();
        services.AddScoped<Autenticacao>();
        services.AddScoped<GestaoDeUsuarios>();
        services.AddScoped<Historico>();

        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioAtual, UsuarioAtual>();
        return services;
    }

    /// <summary>Evita robôs enchendo a agenda ou tentando adivinhar a senha.</summary>
    public static IServiceCollection AddLimitesDeRequisicao(this IServiceCollection services)
    {
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(LimiteDeAgendamento, ctx => PorIp(ctx, limite: 5));
            o.AddPolicy(LimiteDeLogin, ctx => PorIp(ctx, limite: 10));
        });
        return services;
    }

    private static RateLimitPartition<string> PorIp(HttpContext ctx, int limite) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = TimeSpan.FromMinutes(1) });
}
