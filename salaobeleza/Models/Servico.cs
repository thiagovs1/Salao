namespace SalaoBeleza.Models;

// Representa a tabela de servicos oferecidos pelo salao.
public class Servico
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    // decimal e usado para dinheiro (evita erro de arredondamento).
    public decimal Preco { get; set; }

    public int DuracaoMinutos { get; set; }

    public List<Agendamento> Agendamentos { get; set; } = new();
}
