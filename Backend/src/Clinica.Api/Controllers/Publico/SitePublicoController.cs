using Clinica.Api.Configuracao;
using Clinica.Application.Agendamentos;
using Clinica.Application.Cadastros;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Clinica.Api.Controllers.Publico;

// Rotas abertas, usadas pelo site que o cliente vê. Nenhuma exige login.

[ApiController]
[Route("api/clinica")]
public class ClinicaController(Configuracoes configuracoes) : ControllerBase
{
    [HttpGet]
    public Task<ConfiguracaoDto> Obter(CancellationToken ct) => configuracoes.ObterAsync(ct);
}

[ApiController]
[Route("api/servicos")]
public class ServicosController(CadastroDeServicos servicos) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ServicoDto>> Listar(CancellationToken ct) => servicos.ListarAsync(somenteAtivos: true, ct);

    [HttpGet("{id:guid}")]
    public Task<ServicoDto> Obter(Guid id, CancellationToken ct) => servicos.ObterAsync(id, somenteAtivo: true, ct);
}

[ApiController]
[Route("api/disponibilidade")]
public class DisponibilidadeController(AgendamentoOnline agendamentoOnline) : ControllerBase
{
    /// <summary>GET /api/disponibilidade/horarios?servicoId=...&amp;data=2026-10-10 → ["09:00", "09:15", ...]</summary>
    [HttpGet("horarios")]
    public async Task<IEnumerable<string>> Horarios([FromQuery] Guid servicoId, [FromQuery] DateOnly data, CancellationToken ct) =>
        (await agendamentoOnline.ListarHorariosDisponiveisAsync(servicoId, data, ct)).Select(h => h.ToString("HH:mm"));

    /// <summary>GET /api/disponibilidade/profissionais?servicoId=...&amp;inicio=2026-10-10T14:00</summary>
    [HttpGet("profissionais")]
    public Task<IReadOnlyList<ProfissionalResumoDto>> Profissionais([FromQuery] Guid servicoId, [FromQuery] DateTime inicio, CancellationToken ct) =>
        agendamentoOnline.ListarProfissionaisDisponiveisAsync(servicoId, inicio, ct);
}

[ApiController]
[Route("api/agendamentos")]
public class AgendamentosController(AgendamentoOnline agendamentoOnline) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RegistroDaAplicacao.LimiteDeAgendamento)]
    public async Task<ActionResult<AgendamentoCriadoDto>> Criar(CriarAgendamentoDto dados, CancellationToken ct)
    {
        var criado = await agendamentoOnline.CriarAsync(dados, ct);
        return StatusCode(StatusCodes.Status201Created, criado);
    }
}
