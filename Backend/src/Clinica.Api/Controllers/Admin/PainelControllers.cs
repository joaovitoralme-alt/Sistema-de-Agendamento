using Clinica.Application.Agendamentos;
using Clinica.Application.Cadastros;
using Clinica.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.Api.Controllers.Admin;

// Rotas do painel. [Authorize] = só com login.

public sealed record RemarcarDto(DateTime NovoInicio);
public sealed record TrocarProfissionalDto(Guid ProfissionalId);

[ApiController]
[Authorize]
[Route("api/admin/agendamentos")]
public class AdminAgendamentosController(GestaoDeAgendamentos gestao) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AgendamentoDto>> Listar([FromQuery] DateTime de, [FromQuery] DateTime ate,
        [FromQuery] StatusAgendamento? status, [FromQuery] Guid? profissionalId, CancellationToken ct) =>
        gestao.ListarAsync(de, ate, status, profissionalId, ct);

    [HttpPost("{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id, CancellationToken ct)
    {
        await gestao.ConfirmarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
    {
        await gestao.CancelarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/remarcar")]
    public async Task<IActionResult> Remarcar(Guid id, RemarcarDto dados, CancellationToken ct)
    {
        await gestao.RemarcarAsync(id, dados.NovoInicio, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/trocar-profissional")]
    public async Task<IActionResult> TrocarProfissional(Guid id, TrocarProfissionalDto dados, CancellationToken ct)
    {
        await gestao.TrocarProfissionalAsync(id, dados.ProfissionalId, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/profissionais-disponiveis")]
    public Task<IReadOnlyList<ProfissionalResumoDto>> ProfissionaisDisponiveis(Guid id, CancellationToken ct) =>
        gestao.ProfissionaisParaTrocaAsync(id, ct);
}

[ApiController]
[Authorize]
[Route("api/admin/alertas")]
public class AdminAlertasController(GestaoDeAgendamentos gestao) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AlertaDto>> Listar(CancellationToken ct) => gestao.ListarAlertasAsync(ct);
}

[ApiController]
[Authorize]
[Route("api/admin/servicos")]
public class AdminServicosController(CadastroDeServicos servicos) : ControllerBase
{
    private const long TamanhoMaximoDaFoto = 5 * 1024 * 1024;

    [HttpGet]
    public Task<IReadOnlyList<ServicoDto>> Listar(CancellationToken ct) => servicos.ListarAsync(somenteAtivos: false, ct);

    [HttpPost]
    public Task<ServicoDto> Criar(SalvarServicoDto dados, CancellationToken ct) => servicos.CriarAsync(dados, ct);

    [HttpPut("{id:guid}")]
    public Task<ServicoDto> Atualizar(Guid id, SalvarServicoDto dados, CancellationToken ct) => servicos.AtualizarAsync(id, dados, ct);

    [HttpPost("{id:guid}/fotos")]
    [RequestSizeLimit(TamanhoMaximoDaFoto + 1024 * 1024)]
    public async Task<ActionResult<ServicoDto>> AdicionarFoto(Guid id, IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo.Length == 0 || arquivo.Length > TamanhoMaximoDaFoto)
            return BadRequest(new ProblemDetails { Status = 400, Title = "A foto precisa ter até 5 MB." });

        await using var conteudo = arquivo.OpenReadStream();
        return await servicos.AdicionarFotoAsync(id, conteudo, arquivo.FileName, ct);
    }

    [HttpDelete("{id:guid}/fotos")]
    public Task<ServicoDto> RemoverFoto(Guid id, [FromQuery] string caminho, CancellationToken ct) =>
        servicos.RemoverFotoAsync(id, caminho, ct);
}

[ApiController]
[Authorize]
[Route("api/admin/profissionais")]
public class AdminProfissionaisController(CadastroDeProfissionais profissionais) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ProfissionalDto>> Listar(CancellationToken ct) => profissionais.ListarAsync(ct);

    [HttpPost]
    public Task<ProfissionalDto> Criar(SalvarProfissionalDto dados, CancellationToken ct) => profissionais.CriarAsync(dados, ct);

    [HttpPut("{id:guid}")]
    public Task<ProfissionalDto> Atualizar(Guid id, SalvarProfissionalDto dados, CancellationToken ct) =>
        profissionais.AtualizarAsync(id, dados, ct);
}

[ApiController]
[Authorize]
[Route("api/admin/ausencias")]
public class AdminAusenciasController(CadastroDeAusencias ausencias) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AusenciaDto>> Listar([FromQuery] DateTime? de, [FromQuery] DateTime? ate, CancellationToken ct) =>
        ausencias.ListarAsync(de, ate, ct);

    [HttpPost]
    public Task<AusenciaCriadaDto> Criar(SalvarAusenciaDto dados, CancellationToken ct) => ausencias.CriarAsync(dados, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        await ausencias.RemoverAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/admin/configuracao")]
public class AdminConfiguracaoController(Configuracoes configuracoes) : ControllerBase
{
    [HttpGet]
    public Task<ConfiguracaoDto> Obter(CancellationToken ct) => configuracoes.ObterAsync(ct);

    [HttpPut]
    public Task<ConfiguracaoDto> Atualizar(ConfiguracaoDto dados, CancellationToken ct) => configuracoes.AtualizarAsync(dados, ct);
}
