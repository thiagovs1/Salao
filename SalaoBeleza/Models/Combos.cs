namespace SalaoBeleza.Models;

public class Combo
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    public List<ComboServico> ComboServicos { get; set; } = new();
}