using Clinica.Application.Abstracoes;
using Clinica.Application.Auditoria;
using Clinica.Application.Comum;
using Clinica.Domain.Comum;
using Clinica.Domain.Entities;

namespace Clinica.Application.Autenticacao;

public sealed record UsuarioLogadoDto(Guid Id, string Codigo, string Nome, string Email);

/// <summary>Resposta de login: o token JWT, quando ele vence e quem está logado.</summary>
public sealed record SessaoDto(string Token, DateTime ExpiraEmUtc, UsuarioLogadoDto Usuario);

public class Autenticacao(
    IUsuarioRepositorio usuarios,
    IHashDeSenha hash,
    IGeradorDeToken gerador,
    Historico historico,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual,
    IRelogio relogio)
{
    // Mesma mensagem para "não existe" e "senha errada": não revela quais identificadores existem.
    private const string LoginInvalido = "Identificador ou senha incorretos.";

    /// <summary>Entra com o identificador (@a123456@) ou o e-mail, e a senha.</summary>
    public async Task<SessaoDto> EntrarAsync(string login, string senha, CancellationToken ct)
    {
        var usuario = await BuscarAsync(login, ct);

        if (usuario is null || !usuario.Ativo || usuario.SenhaHash is null
            || !hash.Verificar(usuario.SenhaHash, senha ?? string.Empty))
        {
            if (usuario is not null)
            {
                historico.Registrar(usuario, "auth.login_recusado", $"Tentativa de login recusada para {usuario.Nome} ({usuario.Codigo}).", usuario.Id);
                await unidadeDeTrabalho.SalvarAsync(ct);
            }
            throw new NaoAutorizadoException(LoginInvalido);
        }

        usuario.RegistrarAcesso(relogio.Agora);
        historico.Registrar(usuario, "auth.login", "Entrou no painel.", usuario.Id);
        await unidadeDeTrabalho.SalvarAsync(ct);
        return CriarSessao(usuario);
    }

    /// <summary>Primeiro acesso: identificador + código recebido do administrador + senha nova.</summary>
    public async Task<SessaoDto> PrimeiroAcessoAsync(string codigo, string codigoDeAcesso, string novaSenha, CancellationToken ct)
    {
        const string invalido = "Identificador ou código de acesso inválido (ou vencido). Peça um novo ao administrador.";

        var usuario = await usuarios.ObterPorCodigoAsync((codigo ?? string.Empty).Trim().ToLowerInvariant(), ct);
        if (usuario is null || !usuario.Ativo || !usuario.CodigoDeAcessoValido(relogio.Agora)
            || !hash.Verificar(usuario.CodigoDeAcessoHash!, (codigoDeAcesso ?? string.Empty).Trim().ToUpperInvariant()))
            throw new NaoAutorizadoException(invalido);

        Usuario.GarantirSenhaForte(novaSenha);
        usuario.DefinirSenha(hash.Gerar(novaSenha));
        usuario.RegistrarAcesso(relogio.Agora);
        historico.Registrar(usuario, "auth.primeiro_acesso", "Fez o primeiro acesso e criou a senha.", usuario.Id);
        await unidadeDeTrabalho.SalvarAsync(ct);
        return CriarSessao(usuario);
    }

    /// <summary>
    /// Troca a senha do usuário logado. Devolve um token novo: o antigo deixa de valer,
    /// o que também derruba sessões abertas em outros aparelhos.
    /// </summary>
    public async Task<SessaoDto> TrocarSenhaAsync(string senhaAtual, string novaSenha, CancellationToken ct)
    {
        var usuario = await UsuarioLogadoAsync(ct);
        if (usuario.SenhaHash is null || !hash.Verificar(usuario.SenhaHash, senhaAtual ?? string.Empty))
            throw new RegraDeNegocioException("Senha atual incorreta.");

        Usuario.GarantirSenhaForte(novaSenha);
        usuario.DefinirSenha(hash.Gerar(novaSenha));
        historico.Registrar(usuario, "auth.trocar_senha", "Trocou a própria senha.", usuario.Id);
        await unidadeDeTrabalho.SalvarAsync(ct);
        return CriarSessao(usuario);
    }

    public async Task<UsuarioLogadoDto> ObterUsuarioLogadoAsync(CancellationToken ct) => ParaDto(await UsuarioLogadoAsync(ct));

    /// <summary>
    /// Chamado a cada requisição autenticada: o token só vale se o usuário continua ativo
    /// e o selo de segurança ainda é o mesmo (não trocou a senha nem foi desativado).
    /// </summary>
    public async Task<bool> SessaoValidaAsync(Guid usuarioId, Guid selo, CancellationToken ct)
    {
        var usuario = await usuarios.ObterAsync(usuarioId, ct);
        return usuario is { Ativo: true, SenhaHash: not null } && usuario.SeloDeSeguranca == selo;
    }

    private Task<Usuario?> BuscarAsync(string? login, CancellationToken ct)
    {
        var texto = (login ?? string.Empty).Trim().ToLowerInvariant();
        return texto.StartsWith('@') ? usuarios.ObterPorCodigoAsync(texto, ct) : usuarios.ObterPorEmailAsync(texto, ct);
    }

    private async Task<Usuario> UsuarioLogadoAsync(CancellationToken ct) =>
        usuarioAtual.Id is { } id && await usuarios.ObterAsync(id, ct) is { } usuario
            ? usuario
            : throw new NaoAutorizadoException("Sessão inválida. Entre novamente.");

    private SessaoDto CriarSessao(Usuario usuario)
    {
        var token = gerador.Gerar(usuario);
        return new SessaoDto(token.Token, token.ExpiraEmUtc, ParaDto(usuario));
    }

    private static UsuarioLogadoDto ParaDto(Usuario u) => new(u.Id, u.Codigo, u.Nome, u.Email);
}
