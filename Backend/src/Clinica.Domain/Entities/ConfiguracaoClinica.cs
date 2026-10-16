using Clinica.Domain.Comum;

namespace Clinica.Domain.Entities;

/// <summary>
/// Configurações gerais. Existe uma só no sistema (o sistema é instalado por clínica).
/// </summary>
public class ConfiguracaoClinica : Entidade
{
    public static readonly Guid IdUnico = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public string NomeDaClinica { get; private set; } = "Elos de Satisfação";
    public string? WhatsApp { get; private set; }
    public string? Instagram { get; private set; }
    public string? Endereco { get; private set; }

    public bool PermitirEscolherProfissional { get; private set; } = true;

    /// <summary>Dias da semana em que a clínica inteira não abre.</summary>
    public List<DayOfWeek> DiasFechados { get; private set; } = [];

    /// <summary>Com quanta antecedência mínima o cliente pode agendar (ex.: 60 = 1 hora antes).</summary>
    public int AntecedenciaMinimaEmMinutos { get; private set; } = 60;

    /// <summary>Até quantos dias no futuro a agenda fica aberta para o cliente.</summary>
    public int DiasMaximosParaAgendar { get; private set; } = 60;

    private ConfiguracaoClinica() { }

    public static ConfiguracaoClinica Padrao() => new() { Id = IdUnico };

    public void AtualizarDadosDaClinica(string nome, string? whatsApp, string? instagram, string? endereco)
    {
        NomeDaClinica = Validar.Texto(nome, "Nome da clínica", 100);
        WhatsApp = Validar.TextoOpcional(whatsApp, "WhatsApp", 30);
        Instagram = Validar.TextoOpcional(instagram, "Instagram", 100);
        Endereco = Validar.TextoOpcional(endereco, "Endereço", 200);
    }

    public void AtualizarRegrasDeAgenda(bool permitirEscolherProfissional, IEnumerable<DayOfWeek> diasFechados,
        int antecedenciaMinimaEmMinutos, int diasMaximosParaAgendar)
    {
        if (antecedenciaMinimaEmMinutos < 0)
            throw new RegraDeNegocioException("A antecedência mínima não pode ser negativa.");

        if (diasMaximosParaAgendar is < 1 or > 365)
            throw new RegraDeNegocioException("A agenda pode ficar aberta de 1 a 365 dias.");

        PermitirEscolherProfissional = permitirEscolherProfissional;
        DiasFechados = diasFechados.Distinct().OrderBy(d => d).ToList();
        AntecedenciaMinimaEmMinutos = antecedenciaMinimaEmMinutos;
        DiasMaximosParaAgendar = diasMaximosParaAgendar;
    }

    public bool FechadaEm(DayOfWeek dia) => DiasFechados.Contains(dia);
}
