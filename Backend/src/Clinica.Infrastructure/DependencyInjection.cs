using Clinica.Application.Abstracoes;
using Clinica.Domain.Entities;
using Clinica.Infrastructure.Memoria;
using Clinica.Infrastructure.Persistencia;
using Clinica.Infrastructure.Suporte;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure;

/// <summary>
/// Liga cada interface da Application à sua implementação. A Api chama isto no Program.cs.
/// </summary>
public static class DependencyInjection
{
    public static bool UsaMemoria(IConfiguration configuration) =>
        string.Equals(configuration["Banco:Provedor"], "Memoria", StringComparison.OrdinalIgnoreCase);

    public static string PastaDeFotos(IConfiguration configuration) =>
        Path.GetFullPath(configuration["Fotos:Pasta"] ?? Path.Combine("dados", "fotos"));

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        if (UsaMemoria(configuration))
        {
            services.AddSingleton<BancoEmMemoria>();
            services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalhoEmMemoria>();
            services.AddScoped<IServicoRepositorio, ServicoRepositorioEmMemoria>();
            services.AddScoped<IProfissionalRepositorio, ProfissionalRepositorioEmMemoria>();
            services.AddScoped<IAgendamentoRepositorio, AgendamentoRepositorioEmMemoria>();
            services.AddScoped<IAusenciaRepositorio, AusenciaRepositorioEmMemoria>();
            services.AddScoped<IConfiguracaoRepositorio, ConfiguracaoRepositorioEmMemoria>();
            services.AddScoped<IUsuarioRepositorio, UsuarioRepositorioEmMemoria>();
            services.AddScoped<IHistoricoRepositorio, HistoricoRepositorioEmMemoria>();
        }
        else
        {
            RegistroPostgres.Registrar(services, configuration);
        }

        services.AddSingleton<IRelogio>(new RelogioDaClinica(configuration["Clinica:FusoHorario"] ?? "America/Sao_Paulo"));
        services.AddSingleton<IHashDeSenha, HashDeSenha>();
        services.AddSingleton<IArmazenamentoDeFotos>(new ArmazenamentoLocalDeFotos(PastaDeFotos(configuration)));

        return services;
    }

    /// <summary>
    /// Roda uma vez ao iniciar: cria o banco, a configuração padrão, o primeiro administrador
    /// e (se pedido) os dados de exemplo.
    /// </summary>
    public static async Task InicializarInfraestruturaAsync(this IServiceProvider rootServices, IConfiguration configuration)
    {
        using var escopo = rootServices.CreateScope();
        var services = escopo.ServiceProvider;
        var log = services.GetRequiredService<ILoggerFactory>().CreateLogger("Inicializacao");
        var ct = CancellationToken.None;

        if (!UsaMemoria(configuration))
            await RegistroPostgres.CriarBancoSeNecessarioAsync(services, ct);

        await services.GetRequiredService<IConfiguracaoRepositorio>().ObterAsync(ct);
        await services.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(ct);

        await CriarPrimeiroUsuarioAsync(services, configuration, log, ct);

        if (configuration.GetValue<bool>("Banco:DadosDeExemplo"))
            await DadosDeExemplo.CriarSeVazioAsync(services, ct);
    }

    /// <summary>
    /// Se o banco ainda não tem nenhum usuário, cria o primeiro a partir de Admin:Nome/Email/Senha.
    /// Ele já nasce com senha (não passa pelo primeiro acesso) e pode entrar com o e-mail.
    /// </summary>
    private static async Task CriarPrimeiroUsuarioAsync(IServiceProvider services, IConfiguration configuration, ILogger log, CancellationToken ct)
    {
        var usuarios = services.GetRequiredService<IUsuarioRepositorio>();
        if (await usuarios.ExisteAlgumAsync(ct))
            return;

        var email = configuration["Admin:Email"];
        var senha = configuration["Admin:Senha"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            log.LogWarning("Nenhum usuário cadastrado. Defina Admin:Email e Admin:Senha para criar o primeiro.");
            return;
        }

        Usuario.GarantirSenhaForte(senha);
        var relogio = services.GetRequiredService<IRelogio>();
        var usuario = new Usuario(Usuario.GerarCodigo(), configuration["Admin:Nome"] ?? "Administrador", email, relogio.Agora);
        usuario.DefinirSenha(services.GetRequiredService<IHashDeSenha>().Gerar(senha));
        usuarios.Adicionar(usuario);
        services.GetRequiredService<IHistoricoRepositorio>().Adicionar(
            new RegistroDeHistorico(relogio.Agora, null, "usuario.criar", $"Primeiro usuário criado na instalação: {usuario.Nome} ({usuario.Codigo}, {usuario.Email})."));
        await services.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(ct);

        log.LogInformation("Primeiro usuário criado: {Email} (identificador {Codigo}).", usuario.Email, usuario.Codigo);
    }
}
