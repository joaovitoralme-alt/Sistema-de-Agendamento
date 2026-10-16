using Clinica.Application.Abstracoes;
using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Infrastructure.Persistencia;

// Implementações das interfaces da Application usando Entity Framework + PostgreSQL.

public class ServicoRepositorioEf(ClinicaDbContext db) : IServicoRepositorio
{
    public async Task<Servico?> ObterAsync(Guid id, CancellationToken ct) => await db.Servicos.FindAsync([id], ct);
    public Task<List<Servico>> ListarAsync(CancellationToken ct) => db.Servicos.ToListAsync(ct);
    public void Adicionar(Servico servico) => db.Servicos.Add(servico);
}

public class ProfissionalRepositorioEf(ClinicaDbContext db) : IProfissionalRepositorio
{
    public async Task<Profissional?> ObterAsync(Guid id, CancellationToken ct) => await db.Profissionais.FindAsync([id], ct);
    public Task<List<Profissional>> ListarAsync(CancellationToken ct) => db.Profissionais.ToListAsync(ct);
    public void Adicionar(Profissional profissional) => db.Profissionais.Add(profissional);
}

public class AgendamentoRepositorioEf(ClinicaDbContext db) : IAgendamentoRepositorio
{
    public async Task<Agendamento?> ObterAsync(Guid id, CancellationToken ct) => await db.Agendamentos.FindAsync([id], ct);

    public Task<List<Agendamento>> ListarAsync(DateTime de, DateTime ate, CancellationToken ct) =>
        db.Agendamentos.Where(a => a.Inicio < ate && a.FimBloqueado > de).ToListAsync(ct);

    public void Adicionar(Agendamento agendamento) => db.Agendamentos.Add(agendamento);
}

public class AusenciaRepositorioEf(ClinicaDbContext db) : IAusenciaRepositorio
{
    public async Task<Ausencia?> ObterAsync(Guid id, CancellationToken ct) => await db.Ausencias.FindAsync([id], ct);

    public Task<List<Ausencia>> ListarAsync(DateTime de, DateTime ate, CancellationToken ct) =>
        db.Ausencias.Where(a => a.Inicio < ate && a.Fim > de).ToListAsync(ct);

    public void Adicionar(Ausencia ausencia) => db.Ausencias.Add(ausencia);
    public void Remover(Ausencia ausencia) => db.Ausencias.Remove(ausencia);
}

public class ConfiguracaoRepositorioEf(ClinicaDbContext db) : IConfiguracaoRepositorio
{
    public async Task<ConfiguracaoClinica> ObterAsync(CancellationToken ct)
    {
        var configuracao = await db.Configuracoes.FindAsync([ConfiguracaoClinica.IdUnico], ct);
        if (configuracao is not null)
            return configuracao;

        configuracao = ConfiguracaoClinica.Padrao();
        db.Configuracoes.Add(configuracao);
        return configuracao;
    }
}

public class UsuarioRepositorioEf(ClinicaDbContext db) : IUsuarioRepositorio
{
    public async Task<Usuario?> ObterAsync(Guid id, CancellationToken ct) => await db.Usuarios.FindAsync([id], ct);
    public Task<Usuario?> ObterPorCodigoAsync(string codigo, CancellationToken ct) => db.Usuarios.FirstOrDefaultAsync(u => u.Codigo == codigo, ct);
    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) => db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);
    public Task<List<Usuario>> ListarAsync(CancellationToken ct) => db.Usuarios.ToListAsync(ct);
    public Task<bool> ExisteAlgumAsync(CancellationToken ct) => db.Usuarios.AnyAsync(ct);
    public void Adicionar(Usuario usuario) => db.Usuarios.Add(usuario);
}

public class HistoricoRepositorioEf(ClinicaDbContext db) : IHistoricoRepositorio
{
    public void Adicionar(RegistroDeHistorico registro) => db.Historico.Add(registro);

    public async Task<(List<RegistroDeHistorico> Itens, int Total)> ListarAsync(FiltroDeHistorico filtro, CancellationToken ct)
    {
        var consulta = db.Historico.AsNoTracking().AsQueryable();
        if (filtro.De is { } de) consulta = consulta.Where(r => r.DataHora >= de);
        if (filtro.Ate is { } ate) consulta = consulta.Where(r => r.DataHora < ate);
        if (filtro.UsuarioId is { } usuarioId) consulta = consulta.Where(r => r.UsuarioId == usuarioId);
        if (filtro.Busca is { } busca)
        {
            // ILike = "contém", sem diferenciar maiúsculas/minúsculas (PostgreSQL).
            var padrao = $"%{busca}%";
            consulta = consulta.Where(r => EF.Functions.ILike(r.Descricao, padrao) || EF.Functions.ILike(r.Acao, padrao));
        }

        var total = await consulta.CountAsync(ct);
        var itens = await consulta
            .OrderByDescending(r => r.DataHora)
            .Skip((filtro.Pagina - 1) * filtro.TamanhoDaPagina)
            .Take(filtro.TamanhoDaPagina)
            .ToListAsync(ct);
        return (itens, total);
    }
}
