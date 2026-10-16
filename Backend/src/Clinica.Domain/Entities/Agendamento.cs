using Clinica.Domain.Comum;
using Clinica.Domain.Enums;
using Clinica.Domain.ValueObjects;

namespace Clinica.Domain.Entities;

/// <summary>Um pedido de horário feito pelo cliente.</summary>
public class Agendamento : Entidade
{
    public Guid ServicoId { get; private set; }
    public Guid ProfissionalId { get; private set; }
    public DateTime Inicio { get; private set; }

    /// <summary>
    /// Fim do tempo bloqueado (duração + folga), calculado na hora do agendamento.
    /// Guardamos o valor para que mudar a duração do serviço depois não mexa
    /// em agendamentos já marcados.
    /// </summary>
    public DateTime FimBloqueado { get; private set; }

    public string ClienteNome { get; private set; } = string.Empty;
    public string ClienteTelefone { get; private set; } = string.Empty;
    public string? Observacao { get; private set; }
    public StatusAgendamento Status { get; private set; } = StatusAgendamento.Pendente;
    public DateTime CriadoEm { get; private set; }

    private Agendamento() { }

    public Agendamento(Servico servico, Guid profissionalId, DateTime inicio,
        string clienteNome, string clienteTelefone, string? observacao, DateTime criadoEm)
    {
        ServicoId = servico.Id;
        ProfissionalId = profissionalId;
        DefinirHorario(inicio, servico.TempoBloqueado);
        ClienteNome = Validar.Texto(clienteNome, "Nome", 120);
        ClienteTelefone = NormalizarTelefone(clienteTelefone);
        Observacao = Validar.TextoOpcional(observacao, "Observação", 500);
        CriadoEm = Validar.HorarioLocal(criadoEm);
    }

    public Periodo PeriodoBloqueado => new(Inicio, FimBloqueado);

    /// <summary>Pendente e Confirmado ocupam a agenda; Cancelado libera.</summary>
    public bool Ativo => Status != StatusAgendamento.Cancelado;

    public void Confirmar()
    {
        if (Status != StatusAgendamento.Pendente)
            throw new RegraDeNegocioException("Só é possível confirmar agendamentos pendentes.");

        Status = StatusAgendamento.Confirmado;
    }

    public void Cancelar()
    {
        if (Status == StatusAgendamento.Cancelado)
            throw new RegraDeNegocioException("Este agendamento já está cancelado.");

        Status = StatusAgendamento.Cancelado;
    }

    /// <summary>A verificação de disponibilidade é feita antes, pelo caso de uso.</summary>
    public void Remarcar(DateTime novoInicio, TimeSpan tempoBloqueado)
    {
        GarantirAtivo();
        DefinirHorario(novoInicio, tempoBloqueado);
    }

    public void TrocarProfissional(Guid profissionalId)
    {
        GarantirAtivo();
        ProfissionalId = profissionalId;
    }

    private void DefinirHorario(DateTime inicio, TimeSpan tempoBloqueado)
    {
        if (inicio.Minute % HorarioDeTrabalho.GradeEmMinutos != 0 || inicio.Second != 0 || inicio.Millisecond != 0)
            throw new RegraDeNegocioException("O horário precisa ser de 15 em 15 minutos.");

        Inicio = Validar.HorarioLocal(inicio);
        FimBloqueado = Inicio + tempoBloqueado;
    }

    private void GarantirAtivo()
    {
        if (!Ativo)
            throw new RegraDeNegocioException("Este agendamento está cancelado.");
    }

    /// <summary>Guarda só os números e exige DDD + número (10 ou 11 dígitos).</summary>
    private static string NormalizarTelefone(string? telefone)
    {
        var digitos = new string((telefone ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digitos.StartsWith("55") && digitos.Length is 12 or 13)
            digitos = digitos[2..];

        if (digitos.Length is not (10 or 11))
            throw new RegraDeNegocioException("Informe um telefone com DDD.");

        return digitos;
    }
}
