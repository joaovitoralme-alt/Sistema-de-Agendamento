using Clinica.Application.Abstracoes;
using Clinica.Domain.Entities;
using Clinica.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Clinica.Infrastructure;

/// <summary>
/// Procedimentos e profissionais de exemplo, para demonstrar o sistema a um cliente.
/// Só roda se não houver nenhum serviço cadastrado e Banco:DadosDeExemplo = true.
/// </summary>
internal static class DadosDeExemplo
{
    public static async Task CriarSeVazioAsync(IServiceProvider services, CancellationToken ct)
    {
        var servicos = services.GetRequiredService<IServicoRepositorio>();
        if ((await servicos.ListarAsync(ct)).Count > 0)
            return;

        var limpeza = new Servico("Limpeza de Pele",
            "Remove impurezas, cravos e células mortas, deixando a pele renovada e preparada para outros tratamentos.", 45);
        var microagulhamento = new Servico("Microagulhamento",
            "Estimula a produção de colágeno com microperfurações, melhorando textura, manchas e cicatrizes de acne.", 60);
        var drenagem = new Servico("Drenagem Linfática",
            "Massagem suave que estimula o sistema linfático, reduzindo inchaço e retenção de líquidos.", 60);
        var massagem = new Servico("Massagem Relaxante",
            "Movimentos firmes e ritmados que aliviam tensões musculares e o estresse do dia a dia.", 60);
        var preEPos = new Servico("Pré e Pós-operatório",
            "Cuidados que preparam o corpo para a cirurgia e aceleram a recuperação, com drenagem e técnicas específicas.", 60);

        foreach (var servico in new[] { limpeza, microagulhamento, drenagem, massagem, preEPos })
            servicos.Adicionar(servico);

        var semana = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var expediente = semana.SelectMany(dia => new[]
        {
            new HorarioDeTrabalho(dia, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            new HorarioDeTrabalho(dia, new TimeOnly(13, 0), new TimeOnly(18, 0))
        }).ToList();

        var profissionais = services.GetRequiredService<IProfissionalRepositorio>();
        profissionais.Adicionar(new Profissional("Ana", [limpeza.Id, microagulhamento.Id, drenagem.Id],
            [.. expediente, new HorarioDeTrabalho(DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(13, 0))]));
        profissionais.Adicionar(new Profissional("Beatriz", [drenagem.Id, massagem.Id, preEPos.Id], expediente));

        var configuracao = await services.GetRequiredService<IConfiguracaoRepositorio>().ObterAsync(ct);
        configuracao.AtualizarDadosDaClinica("Elos de Satisfação", "11999999999", null, null);
        configuracao.AtualizarRegrasDeAgenda(true, [DayOfWeek.Sunday], 60, 60);

        await services.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(ct);
    }
}
