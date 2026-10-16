using Clinica.Domain.Enums;

namespace Clinica.Application.Agendamentos;

// DTO = "Data Transfer Object": o formato dos dados que entram e saem da aplicação.
// Eles existem para a API não expor as entidades diretamente.

public sealed record CriarAgendamentoDto(
    Guid ServicoId,
    DateTime Inicio,
    Guid? ProfissionalId,
    string ClienteNome,
    string ClienteTelefone,
    string? Observacao);

public sealed record AgendamentoCriadoDto(
    Guid Id,
    DateTime Inicio,
    string ServicoNome,
    string? ProfissionalNome);

public sealed record ProfissionalResumoDto(Guid Id, string Nome);

public sealed record AgendamentoDto(
    Guid Id,
    DateTime Inicio,
    DateTime FimDoAtendimento,
    DateTime FimBloqueado,
    Guid ServicoId,
    string ServicoNome,
    Guid ProfissionalId,
    string ProfissionalNome,
    string ClienteNome,
    string ClienteTelefone,
    string? Observacao,
    StatusAgendamento Status,
    DateTime CriadoEm);

public sealed record AlertaDto(AgendamentoDto Agendamento, string Motivo);
