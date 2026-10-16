using Clinica.Application.Abstracoes;
using Clinica.Application.Auditoria;
using Clinica.Application.Comum;
using Clinica.Domain.Comum;
using Clinica.Domain.Entities;

namespace Clinica.Application.Cadastros;

public sealed record ServicoDto(Guid Id, string Nome, string Descricao, int DuracaoEmMinutos, IReadOnlyList<string> Fotos, bool Ativo);

public sealed record SalvarServicoDto(string Nome, string? Descricao, int DuracaoEmMinutos, bool Ativo = true);

public class CadastroDeServicos(
    IServicoRepositorio servicos,
    IArmazenamentoDeFotos fotos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    Historico historico)
{
    private static readonly string[] ExtensoesPermitidas = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<IReadOnlyList<ServicoDto>> ListarAsync(bool somenteAtivos, CancellationToken ct) =>
        (await servicos.ListarAsync(ct))
            .Where(s => !somenteAtivos || s.Ativo)
            .OrderBy(s => s.Nome)
            .Select(ParaDto)
            .ToList();

    public async Task<ServicoDto> ObterAsync(Guid id, bool somenteAtivo, CancellationToken ct)
    {
        var servico = await ObterEntidadeAsync(id, ct);
        if (somenteAtivo && !servico.Ativo)
            throw new NaoEncontradoException("Procedimento não encontrado.");
        return ParaDto(servico);
    }

    public async Task<ServicoDto> CriarAsync(SalvarServicoDto dados, CancellationToken ct)
    {
        var servico = new Servico(dados.Nome, dados.Descricao ?? string.Empty, dados.DuracaoEmMinutos);
        servico.DefinirAtivo(dados.Ativo);
        servicos.Adicionar(servico);
        await historico.RegistrarAsync("servico.criar", $"Cadastrou o procedimento {servico.Nome} ({Resumo(servico)}).", servico.Id, ct);
        await unidadeDeTrabalho.SalvarAsync(ct);
        return ParaDto(servico);
    }

    public async Task<ServicoDto> AtualizarAsync(Guid id, SalvarServicoDto dados, CancellationToken ct)
    {
        var servico = await ObterEntidadeAsync(id, ct);
        var antes = $"{servico.Nome} ({Resumo(servico)})";
        servico.Atualizar(dados.Nome, dados.Descricao ?? string.Empty, dados.DuracaoEmMinutos);
        servico.DefinirAtivo(dados.Ativo);
        await historico.RegistrarAsync("servico.alterar", $"Alterou o procedimento {antes} → {servico.Nome} ({Resumo(servico)}).", servico.Id, ct);
        await unidadeDeTrabalho.SalvarAsync(ct);
        return ParaDto(servico);
    }

    public async Task<ServicoDto> AdicionarFotoAsync(Guid id, Stream conteudo, string nomeDoArquivo, CancellationToken ct)
    {
        var extensao = Path.GetExtension(nomeDoArquivo).ToLowerInvariant();
        if (!ExtensoesPermitidas.Contains(extensao))
            throw new RegraDeNegocioException("Envie uma imagem JPG, PNG ou WEBP.");

        var servico = await ObterEntidadeAsync(id, ct);
        if (servico.Fotos.Count >= Servico.MaximoDeFotos)
            throw new RegraDeNegocioException($"Cada serviço pode ter no máximo {Servico.MaximoDeFotos} fotos.");

        var caminho = await fotos.SalvarAsync(conteudo, extensao, ct);
        servico.AdicionarFoto(caminho);
        await historico.RegistrarAsync("servico.foto_adicionar", $"Adicionou uma foto ao procedimento {servico.Nome}.", servico.Id, ct);
        await unidadeDeTrabalho.SalvarAsync(ct);
        return ParaDto(servico);
    }

    public async Task<ServicoDto> RemoverFotoAsync(Guid id, string caminho, CancellationToken ct)
    {
        var servico = await ObterEntidadeAsync(id, ct);
        if (servico.RemoverFoto(caminho))
        {
            await historico.RegistrarAsync("servico.foto_remover", $"Removeu uma foto do procedimento {servico.Nome}.", servico.Id, ct);
            await unidadeDeTrabalho.SalvarAsync(ct);
            await fotos.RemoverAsync(caminho, ct);
        }
        return ParaDto(servico);
    }

    private async Task<Servico> ObterEntidadeAsync(Guid id, CancellationToken ct) =>
        await servicos.ObterAsync(id, ct) ?? throw new NaoEncontradoException("Procedimento não encontrado.");

    private static string Resumo(Servico s) => $"{s.DuracaoEmMinutos} min, no site: {Textos.SimNao(s.Ativo)}";

    private static ServicoDto ParaDto(Servico s) =>
        new(s.Id, s.Nome, s.Descricao, s.DuracaoEmMinutos, s.Fotos.ToList(), s.Ativo);
}
