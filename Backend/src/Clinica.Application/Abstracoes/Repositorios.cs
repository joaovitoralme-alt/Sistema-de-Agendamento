using Clinica.Domain.Entities;

namespace Clinica.Application.Abstracoes;

// A Application só conhece estas INTERFACES. Quem sabe falar com o banco de dados
// é a Infrastructure, que implementa cada uma. Assim dá para trocar o banco
// (ou usar uma versão em memória nos testes) sem mexer nas regras.

public interface IServicoRepositorio
{
    Task<Servico?> ObterAsync(Guid id, CancellationToken ct);
    Task<List<Servico>> ListarAsync(CancellationToken ct);
    void Adicionar(Servico servico);
}

public interface IProfissionalRepositorio
{
    Task<Profissional?> ObterAsync(Guid id, CancellationToken ct);
    Task<List<Profissional>> ListarAsync(CancellationToken ct);
    void Adicionar(Profissional profissional);
}

public interface IAgendamentoRepositorio
{
    Task<Agendamento?> ObterAsync(Guid id, CancellationToken ct);

    /// <summary>Agendamentos (de qualquer status) cujo tempo bloqueado cruza o intervalo [de, ate).</summary>
    Task<List<Agendamento>> ListarAsync(DateTime de, DateTime ate, CancellationToken ct);

    void Adicionar(Agendamento agendamento);
}

public interface IAusenciaRepositorio
{
    Task<Ausencia?> ObterAsync(Guid id, CancellationToken ct);

    /// <summary>Ausências que cruzam o intervalo [de, ate).</summary>
    Task<List<Ausencia>> ListarAsync(DateTime de, DateTime ate, CancellationToken ct);

    void Adicionar(Ausencia ausencia);
    void Remover(Ausencia ausencia);
}

public interface IConfiguracaoRepositorio
{
    /// <summary>Sempre devolve uma configuração (cria a padrão se ainda não existir).</summary>
    Task<ConfiguracaoClinica> ObterAsync(CancellationToken ct);
}

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObterAsync(Guid id, CancellationToken ct);
    Task<Usuario?> ObterPorCodigoAsync(string codigo, CancellationToken ct);
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct);
    Task<List<Usuario>> ListarAsync(CancellationToken ct);
    Task<bool> ExisteAlgumAsync(CancellationToken ct);
    void Adicionar(Usuario usuario);
}

public sealed record FiltroDeHistorico(DateTime? De, DateTime? Ate, Guid? UsuarioId, string? Busca, int Pagina, int TamanhoDaPagina);

public interface IHistoricoRepositorio
{
    void Adicionar(RegistroDeHistorico registro);

    /// <summary>Mais recentes primeiro. Devolve a página pedida e o total de registros do filtro.</summary>
    Task<(List<RegistroDeHistorico> Itens, int Total)> ListarAsync(FiltroDeHistorico filtro, CancellationToken ct);
}

/// <summary>Grava de uma vez tudo o que foi alterado (uma "transação").</summary>
public interface IUnidadeDeTrabalho
{
    Task SalvarAsync(CancellationToken ct);
}
