using System.Security.Cryptography;
using Clinica.Application.Abstracoes;
using Clinica.Application.Auditoria;
using Clinica.Application.Comum;
using Clinica.Domain.Comum;
using Clinica.Domain.Entities;

namespace Clinica.Application.Usuarios;

public sealed record UsuarioDto(Guid Id, string Codigo, string Nome, string Email, DateTime CriadoEm, bool Ativo,
    DateTime? UltimoAcessoEm, bool AguardandoPrimeiroAcesso);

public sealed record SalvarUsuarioDto(string Nome, string Email, bool Ativo = true);

/// <summary>
/// Devolvido ao criar um usuário ou gerar novo acesso. O código de acesso só aparece AQUI,
/// uma única vez: o administrador repassa para a pessoa fazer o primeiro acesso.
/// </summary>
public sealed record AcessoGeradoDto(UsuarioDto Usuario, string CodigoDeAcesso, DateTime ExpiraEm);

public class GestaoDeUsuarios(
    IUsuarioRepositorio usuarios,
    IHashDeSenha hash,
    Historico historico,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual,
    IRelogio relogio)
{
    public static readonly TimeSpan ValidadeDoCodigoDeAcesso = TimeSpan.FromDays(7);

    // Sem letras e números que se confundem (O/0, I/1/L).
    private const string Alfabeto = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct) =>
        (await usuarios.ListarAsync(ct)).OrderBy(u => u.Nome).Select(ParaDto).ToList();

    public async Task<AcessoGeradoDto> CriarAsync(SalvarUsuarioDto dados, CancellationToken ct)
    {
        var email = Usuario.NormalizarEmail(dados.Email);
        await GarantirEmailLivreAsync(email, null, ct);

        var usuario = new Usuario(await GerarCodigoUnicoAsync(ct), dados.Nome, email, relogio.Agora);
        usuario.DefinirAtivo(dados.Ativo);
        var codigo = PrepararPrimeiroAcesso(usuario);

        usuarios.Adicionar(usuario);
        await historico.RegistrarAsync("usuario.criar",
            $"Cadastrou o usuário {usuario.Nome} ({usuario.Codigo}, {usuario.Email}).", usuario.Id, ct);
        await unidadeDeTrabalho.SalvarAsync(ct);

        return new AcessoGeradoDto(ParaDto(usuario), codigo, usuario.CodigoDeAcessoExpiraEm!.Value);
    }

    public async Task<UsuarioDto> AtualizarAsync(Guid id, SalvarUsuarioDto dados, CancellationToken ct)
    {
        var usuario = await ObterAsync(id, ct);
        var email = Usuario.NormalizarEmail(dados.Email);
        await GarantirEmailLivreAsync(email, usuario.Id, ct);

        if (!dados.Ativo && usuario.Id == usuarioAtual.Id)
            throw new RegraDeNegocioException("Você não pode desativar o seu próprio usuário.");

        var mudancas = new List<string>();
        if (usuario.Nome != dados.Nome.Trim()) mudancas.Add($"nome de \"{usuario.Nome}\" para \"{dados.Nome.Trim()}\"");
        if (usuario.Email != email) mudancas.Add($"e-mail de {usuario.Email} para {email}");
        if (usuario.Ativo != dados.Ativo) mudancas.Add(dados.Ativo ? "reativou o acesso" : "desativou o acesso");

        usuario.Atualizar(dados.Nome, email);
        usuario.DefinirAtivo(dados.Ativo);

        if (mudancas.Count > 0)
        {
            await historico.RegistrarAsync("usuario.alterar",
                $"Alterou o usuário {usuario.Nome} ({usuario.Codigo}): {string.Join("; ", mudancas)}.", usuario.Id, ct);
            await unidadeDeTrabalho.SalvarAsync(ct);
        }

        return ParaDto(usuario);
    }

    /// <summary>Para quem esqueceu a senha: apaga a senha atual e gera um novo código de primeiro acesso.</summary>
    public async Task<AcessoGeradoDto> GerarNovoAcessoAsync(Guid id, CancellationToken ct)
    {
        var usuario = await ObterAsync(id, ct);
        if (usuario.Id == usuarioAtual.Id)
            throw new RegraDeNegocioException("Para trocar a sua própria senha, use \"Minha conta\".");

        var codigo = PrepararPrimeiroAcesso(usuario);
        await historico.RegistrarAsync("usuario.novo_acesso",
            $"Gerou um novo código de primeiro acesso para {usuario.Nome} ({usuario.Codigo}). A senha anterior deixou de valer.", usuario.Id, ct);
        await unidadeDeTrabalho.SalvarAsync(ct);

        return new AcessoGeradoDto(ParaDto(usuario), codigo, usuario.CodigoDeAcessoExpiraEm!.Value);
    }

    private string PrepararPrimeiroAcesso(Usuario usuario)
    {
        var codigo = RandomNumberGenerator.GetString(Alfabeto, 8);
        usuario.PrepararPrimeiroAcesso(hash.Gerar(codigo), relogio.Agora + ValidadeDoCodigoDeAcesso);
        return codigo;
    }

    private async Task<string> GerarCodigoUnicoAsync(CancellationToken ct)
    {
        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            var codigo = Usuario.GerarCodigo();
            if (await usuarios.ObterPorCodigoAsync(codigo, ct) is null)
                return codigo;
        }
        throw new InvalidOperationException("Não foi possível gerar um identificador único.");
    }

    private async Task GarantirEmailLivreAsync(string email, Guid? ignorar, CancellationToken ct)
    {
        var existente = await usuarios.ObterPorEmailAsync(email, ct);
        if (existente is not null && existente.Id != ignorar)
            throw new RegraDeNegocioException("Já existe um usuário com este e-mail.");
    }

    private async Task<Usuario> ObterAsync(Guid id, CancellationToken ct) =>
        await usuarios.ObterAsync(id, ct) ?? throw new NaoEncontradoException("Usuário não encontrado.");

    internal static UsuarioDto ParaDto(Usuario u) =>
        new(u.Id, u.Codigo, u.Nome, u.Email, u.CriadoEm, u.Ativo, u.UltimoAcessoEm, u.PrecisaDePrimeiroAcesso);
}
