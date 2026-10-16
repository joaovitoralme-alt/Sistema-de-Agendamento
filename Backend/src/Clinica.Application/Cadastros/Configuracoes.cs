using Clinica.Application.Abstracoes;
using Clinica.Application.Auditoria;
using Clinica.Application.Comum;

namespace Clinica.Application.Cadastros;

public sealed record ConfiguracaoDto(
    string NomeDaClinica,
    string? WhatsApp,
    string? Instagram,
    string? Endereco,
    bool PermitirEscolherProfissional,
    IReadOnlyList<DayOfWeek> DiasFechados,
    int AntecedenciaMinimaEmMinutos,
    int DiasMaximosParaAgendar);

public class Configuracoes(IConfiguracaoRepositorio configuracoes, IUnidadeDeTrabalho unidadeDeTrabalho, Historico historico)
{
    public async Task<ConfiguracaoDto> ObterAsync(CancellationToken ct)
    {
        var c = await configuracoes.ObterAsync(ct);
        return new ConfiguracaoDto(c.NomeDaClinica, c.WhatsApp, c.Instagram, c.Endereco,
            c.PermitirEscolherProfissional, c.DiasFechados.ToList(), c.AntecedenciaMinimaEmMinutos, c.DiasMaximosParaAgendar);
    }

    public async Task<ConfiguracaoDto> AtualizarAsync(ConfiguracaoDto dados, CancellationToken ct)
    {
        var antes = await ObterAsync(ct);
        var c = await configuracoes.ObterAsync(ct);
        c.AtualizarDadosDaClinica(dados.NomeDaClinica, dados.WhatsApp, dados.Instagram, dados.Endereco);
        c.AtualizarRegrasDeAgenda(dados.PermitirEscolherProfissional, dados.DiasFechados ?? [],
            dados.AntecedenciaMinimaEmMinutos, dados.DiasMaximosParaAgendar);

        var depois = await ObterAsync(ct);
        var mudancas = Diferencas(antes, depois);
        if (mudancas.Count > 0)
        {
            await historico.RegistrarAsync("configuracao.alterar", $"Alterou as configurações: {string.Join("; ", mudancas)}.", null, ct);
            await unidadeDeTrabalho.SalvarAsync(ct);
        }
        return depois;
    }

    private static List<string> Diferencas(ConfiguracaoDto a, ConfiguracaoDto b)
    {
        var lista = new List<string>();
        void Comparar(string campo, object? x, object? y)
        {
            if (!Equals(x, y)) lista.Add($"{campo} de \"{x ?? "-"}\" para \"{y ?? "-"}\"");
        }
        Comparar("nome", a.NomeDaClinica, b.NomeDaClinica);
        Comparar("WhatsApp", a.WhatsApp, b.WhatsApp);
        Comparar("Instagram", a.Instagram, b.Instagram);
        Comparar("endereço", a.Endereco, b.Endereco);
        Comparar("escolher profissional", Textos.SimNao(a.PermitirEscolherProfissional), Textos.SimNao(b.PermitirEscolherProfissional));
        Comparar("dias fechados", string.Join(", ", a.DiasFechados), string.Join(", ", b.DiasFechados));
        Comparar("antecedência mínima (min)", a.AntecedenciaMinimaEmMinutos, b.AntecedenciaMinimaEmMinutos);
        Comparar("agenda aberta (dias)", a.DiasMaximosParaAgendar, b.DiasMaximosParaAgendar);
        return lista;
    }
}
