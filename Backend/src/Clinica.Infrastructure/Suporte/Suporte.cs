using Clinica.Application.Abstracoes;
using Clinica.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Clinica.Infrastructure.Suporte;

/// <summary>Hora atual no fuso da clínica (padrão: America/Sao_Paulo).</summary>
public class RelogioDaClinica(string fusoHorario) : IRelogio
{
    private readonly TimeZoneInfo _fuso = TimeZoneInfo.FindSystemTimeZoneById(fusoHorario);

    public DateTime Agora =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _fuso), DateTimeKind.Unspecified);
}

/// <summary>Usa o algoritmo de hash de senha do próprio ASP.NET Core Identity (PBKDF2 com sal).</summary>
public class HashDeSenha : IHashDeSenha
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Gerar(string senha) => _hasher.HashPassword(null!, senha);

    public bool Verificar(string hash, string senha) =>
        _hasher.VerifyHashedPassword(null!, hash, senha) != PasswordVerificationResult.Failed;
}

/// <summary>Guarda as fotos numa pasta do servidor. A API publica essa pasta em /fotos.</summary>
public class ArmazenamentoLocalDeFotos(string pasta) : IArmazenamentoDeFotos
{
    public const string CaminhoPublico = "/fotos";

    public async Task<string> SalvarAsync(Stream conteudo, string extensao, CancellationToken ct)
    {
        Directory.CreateDirectory(pasta);
        var nome = $"{Guid.NewGuid():N}{extensao}";

        await using (var arquivo = File.Create(Path.Combine(pasta, nome)))
            await conteudo.CopyToAsync(arquivo, ct);

        return $"{CaminhoPublico}/{nome}";
    }

    public Task RemoverAsync(string caminhoPublico, CancellationToken ct)
    {
        // GetFileName impede caminhos como "../../arquivo-importante".
        var caminho = Path.Combine(pasta, Path.GetFileName(caminhoPublico));
        if (File.Exists(caminho))
            File.Delete(caminho);
        return Task.CompletedTask;
    }
}
