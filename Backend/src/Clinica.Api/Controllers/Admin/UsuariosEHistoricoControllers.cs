using Clinica.Application.Auditoria;
using Clinica.Application.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/usuarios")]
public class AdminUsuariosController(GestaoDeUsuarios usuarios) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UsuarioDto>> Listar(CancellationToken ct) => usuarios.ListarAsync(ct);

    /// <summary>Cria o usuário e devolve o código de primeiro acesso (mostrado uma única vez).</summary>
    [HttpPost]
    public Task<AcessoGeradoDto> Criar(SalvarUsuarioDto dados, CancellationToken ct) => usuarios.CriarAsync(dados, ct);

    [HttpPut("{id:guid}")]
    public Task<UsuarioDto> Atualizar(Guid id, SalvarUsuarioDto dados, CancellationToken ct) => usuarios.AtualizarAsync(id, dados, ct);

    /// <summary>Apaga a senha atual e gera um novo código de primeiro acesso ("esqueci a senha").</summary>
    [HttpPost("{id:guid}/novo-acesso")]
    public Task<AcessoGeradoDto> NovoAcesso(Guid id, CancellationToken ct) => usuarios.GerarNovoAcessoAsync(id, ct);
}

[ApiController]
[Authorize]
[Route("api/admin/historico")]
public class AdminHistoricoController(Historico historico) : ControllerBase
{
    [HttpGet]
    public Task<PaginaDeHistoricoDto> Listar([FromQuery] DateTime? de, [FromQuery] DateTime? ate, [FromQuery] Guid? usuarioId,
        [FromQuery] string? busca, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 30, CancellationToken ct = default) =>
        historico.ListarAsync(de, ate, usuarioId, busca, pagina, tamanho, ct);
}
