using Clinica.Application.Abstracoes;
using Clinica.Domain.Entities;

namespace Clinica.Application.Auditoria;

public sealed record RegistroDeHistoricoDto(Guid Id, DateTime DataHora, Guid? UsuarioId, string UsuarioCodigo, string UsuarioNome,
    string Acao, string Descricao, Guid? AlvoId);

public sealed record PaginaDeHistoricoDto(IReadOnlyList<RegistroDeHistoricoDto> Itens, int Total, int Pagina, int TamanhoDaPagina);

/// <summary>
/// Registra no histórico o que o usuário logado fez. O registro entra na MESMA gravação
/// da alteração (unidade de trabalho): ou salva as duas coisas, ou nenhuma.
/// </summary>
public class Historico(IHistoricoRepositorio registros, IUsuarioRepositorio usuarios, IUsuarioAtual usuarioAtual, IRelogio relogio)
{
    private Usuario? _usuarioLogado;
    private bool _carregado;

    /// <summary>Registra uma ação feita pelo usuário logado nesta requisição.</summary>
    public async Task RegistrarAsync(string acao, string descricao, Guid? alvoId, CancellationToken ct)
    {
        Registrar(await UsuarioLogadoAsync(ct), acao, descricao, alvoId);
    }

    /// <summary>Registra uma ação de um usuário específico (ex.: login, quando ainda não há token).</summary>
    public void Registrar(Usuario? usuario, string acao, string descricao, Guid? alvoId = null) =>
        registros.Adicionar(new RegistroDeHistorico(relogio.Agora, usuario, acao, descricao, alvoId));

    public async Task<PaginaDeHistoricoDto> ListarAsync(DateTime? de, DateTime? ate, Guid? usuarioId, string? busca,
        int pagina, int tamanhoDaPagina, CancellationToken ct)
    {
        pagina = Math.Max(1, pagina);
        tamanhoDaPagina = Math.Clamp(tamanhoDaPagina, 1, 100);

        var (itens, total) = await registros.ListarAsync(
            new FiltroDeHistorico(de, ate, usuarioId, string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(), pagina, tamanhoDaPagina), ct);

        return new PaginaDeHistoricoDto(
            itens.Select(r => new RegistroDeHistoricoDto(r.Id, r.DataHora, r.UsuarioId, r.UsuarioCodigo, r.UsuarioNome, r.Acao, r.Descricao, r.AlvoId)).ToList(),
            total, pagina, tamanhoDaPagina);
    }

    private async Task<Usuario?> UsuarioLogadoAsync(CancellationToken ct)
    {
        if (!_carregado)
        {
            _usuarioLogado = usuarioAtual.Id is { } id ? await usuarios.ObterAsync(id, ct) : null;
            _carregado = true;
        }
        return _usuarioLogado;
    }
}
