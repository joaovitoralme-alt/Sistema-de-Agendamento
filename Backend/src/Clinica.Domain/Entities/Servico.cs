using Clinica.Domain.Comum;

namespace Clinica.Domain.Entities;

/// <summary>Um procedimento oferecido pela clínica (ex.: Limpeza de Pele).</summary>
public class Servico : Entidade
{
    /// <summary>Minutos livres depois de cada atendimento (limpeza da sala, preparo).</summary>
    public const int FolgaEmMinutos = 15;
    public const int MaximoDeFotos = 10;

    public string Nome { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public int DuracaoEmMinutos { get; private set; }
    public List<string> Fotos { get; private set; } = [];
    public bool Ativo { get; private set; } = true;

    // Construtor vazio e privado: só o Entity Framework usa, para montar o objeto vindo do banco.
    private Servico() { }

    public Servico(string nome, string descricao, int duracaoEmMinutos)
    {
        Atualizar(nome, descricao, duracaoEmMinutos);
    }

    /// <summary>Tempo que o serviço ocupa na agenda: duração + folga.</summary>
    public TimeSpan TempoBloqueado => TimeSpan.FromMinutes(DuracaoEmMinutos + FolgaEmMinutos);

    public void Atualizar(string nome, string descricao, int duracaoEmMinutos)
    {
        if (duracaoEmMinutos <= 0 || duracaoEmMinutos % 15 != 0)
            throw new RegraDeNegocioException("A duração precisa ser múltiplo de 15 minutos.");

        if (duracaoEmMinutos > 8 * 60)
            throw new RegraDeNegocioException("A duração máxima é de 8 horas.");

        Nome = Validar.Texto(nome, "Nome", 100);
        Descricao = Validar.Texto(descricao, "Descrição", 4000, obrigatorio: false);
        DuracaoEmMinutos = duracaoEmMinutos;
    }

    public void DefinirAtivo(bool ativo) => Ativo = ativo;

    public void AdicionarFoto(string caminho)
    {
        if (Fotos.Count >= MaximoDeFotos)
            throw new RegraDeNegocioException($"Cada serviço pode ter no máximo {MaximoDeFotos} fotos.");

        Fotos.Add(caminho);
    }

    public bool RemoverFoto(string caminho) => Fotos.Remove(caminho);
}
