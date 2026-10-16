using Clinica.Application.Abstracoes;
using Clinica.Domain.Entities;

namespace Clinica.Infrastructure.Memoria;

/// <summary>
/// "Banco" que vive só na memória. Some ao reiniciar. Serve para demonstrar o sistema
/// sem instalar o PostgreSQL (Banco:Provedor = "Memoria" no appsettings).
/// Repare que a Application funciona igual: ela só conhece as interfaces.
/// </summary>
public class BancoEmMemoria
{
    internal readonly object Trava = new();
    internal readonly List<Servico> Servicos = [];
    internal readonly List<Profissional> Profissionais = [];
    internal readonly List<Agendamento> Agendamentos = [];
    internal readonly List<Ausencia> Ausencias = [];
    internal readonly List<Usuario> Usuarios = [];
    internal readonly List<RegistroDeHistorico> Historico = [];
    internal readonly ConfiguracaoClinica Configuracao = ConfiguracaoClinica.Padrao();

    internal T Ler<T>(Func<T> leitura) { lock (Trava) return leitura(); }
    internal void Escrever(Action escrita) { lock (Trava) escrita(); }
}

public class ServicoRepositorioEmMemoria(BancoEmMemoria banco) : IServicoRepositorio
{
    public Task<Servico?> ObterAsync(Guid id, CancellationToken ct) => Task.FromResult(banco.Ler(() => banco.Servicos.FirstOrDefault(s => s.Id == id)));
    public Task<List<Servico>> ListarAsync(CancellationToken ct) => Task.FromResult(banco.Ler(() => banco.Servicos.ToList()));
    public void Adicionar(Servico servico) => banco.Escrever(() => banco.Servicos.Add(servico));
}

public class ProfissionalRepositorioEmMemoria(BancoEmMemoria banco) : IProfissionalRepositorio
{
    public Task<Profissional?> ObterAsync(Guid id, CancellationToken ct) => Task.FromResult(banco.Ler(() => banco.Profissionais.FirstOrDefault(p => p.Id == id)));
    public Task<List<Profissional>> ListarAsync(CancellationToken ct) => Task.FromResult(banco.Ler(() => banco.Profissionais.ToList()));
    public void Adicionar(Profissional profissional) => banco.Escrever(() => banco.Profissionais.Add(profissional));
}

public class AgendamentoRepositorioEmMemoria(BancoEmMemoria banco) : IAgendamentoRepositorio
{
    public Task<Agendamento?> ObterAsync(Guid id, CancellationToken ct) => Task.FromResult(banco.Ler(() => banco.Agendamentos.FirstOrDefault(a => a.Id == id)));

    public Task<List<Agendamento>> ListarAsync(DateTime de, DateTime ate, CancellationToken ct) =>
        Task.FromResult(banco.Ler(() => banco.Agendamentos.Where(a => a.Inicio < ate && a.FimBloqueado > de).ToList()));

    public void Adicionar(Agendamento agendamento) => banco.Escrever(() => banco.Agendamentos.Add(agendamento));
}

public class AusenciaRepositorioEmMemoria(BancoEmMemoria banco) : IAusenciaRepositorio
{
    public Task<Ausencia?> ObterAsync(Guid id, CancellationToken ct) => Task.FromResult(banco.Ler(() => banco.Ausencias.FirstOrDefault(a => a.Id == id)));

    public Task<List<Ausencia>> ListarAsync(DateTime de, DateTime ate, CancellationToken ct) =>
        Task.FromResult(banco.Ler(() => banco.Ausencias.Where(a => a.Inicio < ate && a.Fim > de).ToList()));

    public void Adicionar(Ausencia ausencia) => banco.Escrever(() => banco.Ausencias.Add(ausencia));
    public void Remover(Ausencia ausencia) => banco.Escrever(() => banco.Ausencias.Remove(ausencia));
}

public class ConfiguracaoRepositorioEmMemoria(BancoEmMemoria banco) : IConfiguracaoRepositorio
{
    public Task<ConfiguracaoClinica> ObterAsync(CancellationToken ct) => Task.FromResult(banco.Configuracao);
}

public class UsuarioRepositorioEmMemoria(BancoEmMemoria banco) : IUsuarioRepositorio
{
    public Task<Usuario?> ObterAsync(Guid id, CancellationToken ct) => Task.FromResult(banco.Ler(() => banco.Usuarios.FirstOrDefault(u => u.Id == id)));
    public Task<Usuario?> ObterPorCodigoAsync(string codigo, CancellationToken ct) => Task.FromResult(banco.Ler(() => banco.Usuarios.FirstOrDefault(u => u.Codigo == codigo)));
    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) => Task.FromResult(banco.Ler(() => banco.Usuarios.FirstOrDefault(u => u.Email == email)));
    public Task<List<Usuario>> ListarAsync(CancellationToken ct) => Task.FromResult(banco.Ler(() => banco.Usuarios.ToList()));
    public Task<bool> ExisteAlgumAsync(CancellationToken ct) => Task.FromResult(banco.Ler(() => banco.Usuarios.Count > 0));
    public void Adicionar(Usuario usuario) => banco.Escrever(() => banco.Usuarios.Add(usuario));
}

public class HistoricoRepositorioEmMemoria(BancoEmMemoria banco) : IHistoricoRepositorio
{
    public void Adicionar(RegistroDeHistorico registro) => banco.Escrever(() => banco.Historico.Add(registro));

    public Task<(List<RegistroDeHistorico> Itens, int Total)> ListarAsync(FiltroDeHistorico filtro, CancellationToken ct) =>
        Task.FromResult(banco.Ler(() =>
        {
            var consulta = banco.Historico.AsEnumerable();
            if (filtro.De is { } de) consulta = consulta.Where(r => r.DataHora >= de);
            if (filtro.Ate is { } ate) consulta = consulta.Where(r => r.DataHora < ate);
            if (filtro.UsuarioId is { } usuarioId) consulta = consulta.Where(r => r.UsuarioId == usuarioId);
            if (filtro.Busca is { } busca)
                consulta = consulta.Where(r => r.Descricao.Contains(busca, StringComparison.OrdinalIgnoreCase) || r.Acao.Contains(busca, StringComparison.OrdinalIgnoreCase));

            var lista = consulta.OrderByDescending(r => r.DataHora).ToList();
            return (lista.Skip((filtro.Pagina - 1) * filtro.TamanhoDaPagina).Take(filtro.TamanhoDaPagina).ToList(), lista.Count);
        }));
}

/// <summary>Na memória as alterações já valem na hora; não há nada a gravar.</summary>
public class UnidadeDeTrabalhoEmMemoria : IUnidadeDeTrabalho
{
    public Task SalvarAsync(CancellationToken ct) => Task.CompletedTask;
}
