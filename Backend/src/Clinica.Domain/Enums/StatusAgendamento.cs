namespace Clinica.Domain.Enums;

// Os números são explícitos de propósito: o banco de dados vai guardar 1, 2 ou 3.
// Se um dia alguém reordenar os nomes, os registros antigos continuam com o significado certo.
// Começar em 1 evita que o valor padrão de um enum (0) passe por um status válido sem querer.
public enum StatusAgendamento
{
    Pendente = 1,
    Confirmado = 2,
    Cancelado = 3
}
