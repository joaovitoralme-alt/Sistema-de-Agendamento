using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Clinica.Domain.Comum;

namespace Clinica.Domain.Entities;

/// <summary>
/// Quem acessa o painel. Nasce sem senha: no primeiro acesso a pessoa usa um
/// código temporário (que o administrador repassa) e cria a própria senha.
/// </summary>
public partial class Usuario : Entidade
{
    public const int TamanhoMinimoDaSenha = 8;

    /// <summary>Identificador de login no formato @a123456@.</summary>
    public string Codigo { get; private set; } = string.Empty;
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }
    public bool Ativo { get; private set; } = true;
    public DateTime? UltimoAcessoEm { get; private set; }

    /// <summary>Hash da senha. Nulo = ainda não fez o primeiro acesso.</summary>
    public string? SenhaHash { get; private set; }

    /// <summary>Hash do código de primeiro acesso (o código em si nunca é guardado).</summary>
    public string? CodigoDeAcessoHash { get; private set; }
    public DateTime? CodigoDeAcessoExpiraEm { get; private set; }

    /// <summary>
    /// Muda sempre que a senha muda ou o usuário é desativado. Vai dentro do token:
    /// se não bater mais, o token antigo deixa de valer (ex.: sessão aberta em outro computador).
    /// </summary>
    public Guid SeloDeSeguranca { get; private set; } = Guid.NewGuid();

    private Usuario() { }

    public Usuario(string codigo, string nome, string email, DateTime criadoEm)
    {
        if (!CodigoValido(codigo))
            throw new RegraDeNegocioException("Identificador inválido.");

        Codigo = codigo;
        Atualizar(nome, email);
        CriadoEm = Validar.HorarioLocal(criadoEm);
    }

    public bool PrecisaDePrimeiroAcesso => SenhaHash is null;

    public void Atualizar(string nome, string email)
    {
        Nome = Validar.Texto(nome, "Nome", 120);
        Email = NormalizarEmail(email);
    }

    public void DefinirAtivo(bool ativo)
    {
        if (Ativo != ativo)
            SeloDeSeguranca = Guid.NewGuid();
        Ativo = ativo;
    }

    /// <summary>Apaga a senha e prepara um novo primeiro acesso (cadastro novo ou "esqueci a senha").</summary>
    public void PrepararPrimeiroAcesso(string codigoDeAcessoHash, DateTime expiraEm)
    {
        SenhaHash = null;
        CodigoDeAcessoHash = codigoDeAcessoHash;
        CodigoDeAcessoExpiraEm = Validar.HorarioLocal(expiraEm);
        SeloDeSeguranca = Guid.NewGuid();
    }

    public bool CodigoDeAcessoValido(DateTime agora) =>
        CodigoDeAcessoHash is not null && CodigoDeAcessoExpiraEm >= agora;

    public void DefinirSenha(string senhaHash)
    {
        SenhaHash = senhaHash;
        CodigoDeAcessoHash = null;
        CodigoDeAcessoExpiraEm = null;
        SeloDeSeguranca = Guid.NewGuid();
    }

    public void RegistrarAcesso(DateTime agora) => UltimoAcessoEm = Validar.HorarioLocal(agora);

    // ---------- Regras do identificador e da senha ----------

    /// <summary>Gera um identificador novo, ex.: @a406070@.</summary>
    public static string GerarCodigo() => $"@a{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}@";

    public static bool CodigoValido(string? codigo) => codigo is not null && FormatoDoCodigo().IsMatch(codigo);

    public static void GarantirSenhaForte(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < TamanhoMinimoDaSenha)
            throw new RegraDeNegocioException($"A senha precisa ter pelo menos {TamanhoMinimoDaSenha} caracteres.");
    }

    public static string NormalizarEmail(string? email)
    {
        var limpo = Validar.Texto(email, "E-mail", 200).ToLowerInvariant();
        if (!EmailSimples().IsMatch(limpo))
            throw new RegraDeNegocioException("E-mail inválido.");
        return limpo;
    }

    [GeneratedRegex(@"^@a\d{6}@$")]
    private static partial Regex FormatoDoCodigo();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailSimples();
}
