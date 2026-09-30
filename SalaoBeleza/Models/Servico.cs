namespace SalaoBeleza.Models;

public class Servico
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    public int DuracaoMinutos { get; set; }

    public List<Agendamento> Agendamentos { get; set; } = new();
}