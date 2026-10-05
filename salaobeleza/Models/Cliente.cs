namespace SalaoBeleza.Models;

// Representa a tabela de clientes no banco.
// Cada propriedade vira uma coluna.
public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Um cliente pode ter varios agendamentos.
    public List<Agendamento> Agendamentos { get; set; } = new();
}
