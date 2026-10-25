using System.ComponentModel.DataAnnotations.Schema;

namespace SalaoBeleza.Models;

[Table("Clientes")]
public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CPF { get; set; }
    public string? Endereco { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    public List<Agendamento> Agendamentos { get; set; } = new();
}