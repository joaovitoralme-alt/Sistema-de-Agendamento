using Clinica.Application.Abstracoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clinica.Infrastructure.Persistencia;

internal static class RegistroPostgres
{
    public static void Registrar(IServiceCollection services, IConfiguration configuration)
    {
        var conexao = configuration.GetConnectionString("Clinica")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Clinica no appsettings.");

        services.AddDbContext<ClinicaDbContext>(o => o.UseNpgsql(conexao));
        services.AddScoped<IUnidadeDeTrabalho>(sp => sp.GetRequiredService<ClinicaDbContext>());
        services.AddScoped<IServicoRepositorio, ServicoRepositorioEf>();
        services.AddScoped<IProfissionalRepositorio, ProfissionalRepositorioEf>();
        services.AddScoped<IAgendamentoRepositorio, AgendamentoRepositorioEf>();
        services.AddScoped<IAusenciaRepositorio, AusenciaRepositorioEf>();
        services.AddScoped<IConfiguracaoRepositorio, ConfiguracaoRepositorioEf>();
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorioEf>();
        services.AddScoped<IHistoricoRepositorio, HistoricoRepositorioEf>();
    }

    /// <summary>
    /// Cria o banco e as tabelas se ainda não existirem.
    /// Quando o sistema estiver em produção, o próximo passo é trocar isso por Migrations
    /// (dotnet ef migrations add ...), que sabem ALTERAR tabelas sem perder dados.
    /// </summary>
    public static Task CriarBancoSeNecessarioAsync(IServiceProvider services, CancellationToken ct) =>
        services.GetRequiredService<ClinicaDbContext>().Database.EnsureCreatedAsync(ct);
}
