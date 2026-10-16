using Clinica.Application.Abstracoes;
using Clinica.Domain.Entities;
using Clinica.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Infrastructure.Persistencia;

/// <summary>
/// O "mapa" entre as entidades do domínio e as tabelas do PostgreSQL.
/// Também é a unidade de trabalho: SaveChanges grava tudo numa transação só.
/// </summary>
public class ClinicaDbContext(DbContextOptions<ClinicaDbContext> options) : DbContext(options), IUnidadeDeTrabalho
{
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Ausencia> Ausencias => Set<Ausencia>();
    public DbSet<ConfiguracaoClinica> Configuracoes => Set<ConfiguracaoClinica>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RegistroDeHistorico> Historico => Set<RegistroDeHistorico>();

    public Task SalvarAsync(CancellationToken ct) => SaveChangesAsync(ct);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Todas as datas são o horário local da clínica, sem fuso.
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Servico>(e =>
        {
            e.ToTable("servicos");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedNever();
            e.Property(s => s.Nome).HasMaxLength(100).IsRequired();
            e.Property(s => s.Descricao).HasMaxLength(4000);
            e.Property(s => s.Fotos).HasConversion(ConversorJson.Conversor<List<string>>(), ConversorJson.Comparador<List<string>>())
                .HasColumnType("jsonb");
            e.Ignore(s => s.TempoBloqueado);
        });

        modelBuilder.Entity<Profissional>(e =>
        {
            e.ToTable("profissionais");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            e.Property(p => p.ServicoIds).HasConversion(ConversorJson.Conversor<List<Guid>>(), ConversorJson.Comparador<List<Guid>>())
                .HasColumnType("jsonb");
            e.Property(p => p.Horarios).HasConversion(ConversorJson.Conversor<List<HorarioDeTrabalho>>(), ConversorJson.Comparador<List<HorarioDeTrabalho>>())
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<Agendamento>(e =>
        {
            e.ToTable("agendamentos");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedNever();
            e.Property(a => a.ClienteNome).HasMaxLength(120).IsRequired();
            e.Property(a => a.ClienteTelefone).HasMaxLength(20).IsRequired();
            e.Property(a => a.Observacao).HasMaxLength(500);
            e.HasIndex(a => new { a.Inicio, a.FimBloqueado });
            e.HasIndex(a => a.ProfissionalId);
            e.Ignore(a => a.PeriodoBloqueado);
            e.Ignore(a => a.Ativo);
        });

        modelBuilder.Entity<Ausencia>(e =>
        {
            e.ToTable("ausencias");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedNever();
            e.Property(a => a.Descricao).HasMaxLength(200);
            e.HasIndex(a => new { a.Inicio, a.Fim });
            e.Ignore(a => a.Periodo);
            e.Ignore(a => a.ValeParaClinicaToda);
        });

        modelBuilder.Entity<ConfiguracaoClinica>(e =>
        {
            e.ToTable("configuracao");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedNever();
            e.Property(c => c.NomeDaClinica).HasMaxLength(100).IsRequired();
            e.Property(c => c.WhatsApp).HasMaxLength(30);
            e.Property(c => c.Instagram).HasMaxLength(100);
            e.Property(c => c.Endereco).HasMaxLength(200);
            e.Property(c => c.DiasFechados).HasConversion(ConversorJson.Conversor<List<DayOfWeek>>(), ConversorJson.Comparador<List<DayOfWeek>>())
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedNever();
            e.Property(u => u.Codigo).HasMaxLength(9).IsRequired();
            e.Property(u => u.Nome).HasMaxLength(120).IsRequired();
            e.Property(u => u.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(u => u.Codigo).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Ignore(u => u.PrecisaDePrimeiroAcesso);
        });

        modelBuilder.Entity<RegistroDeHistorico>(e =>
        {
            e.ToTable("historico");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).ValueGeneratedNever();
            e.Property(r => r.UsuarioCodigo).HasMaxLength(9).IsRequired();
            e.Property(r => r.UsuarioNome).HasMaxLength(120).IsRequired();
            e.Property(r => r.Acao).HasMaxLength(60).IsRequired();
            e.Property(r => r.Descricao).HasMaxLength(1000).IsRequired();
            e.HasIndex(r => r.DataHora);
            e.HasIndex(r => r.UsuarioId);
        });
    }
}
