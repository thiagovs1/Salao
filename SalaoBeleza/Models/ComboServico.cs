namespace SalaoBeleza.Models;

public class ComboServico
{
    public int ComboId { get; set; }

    public int ServicoId { get; set; }

    public Combo Combo { get; set; } = null!;

    public Servico Servico { get; set; } = null!;
}