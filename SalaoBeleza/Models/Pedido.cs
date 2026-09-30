namespace SalaoBeleza.Models;

public class Pedido
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public int? ComboId { get; set; }

    public string Status { get; set; } = "Em andamento";

    public DateTime DataCriacao { get; set; }

    public Cliente Cliente { get; set; } = null!;

    public Combo? Combo { get; set; }

    public List<PedidoServico> PedidoServicos { get; set; } = new();
}