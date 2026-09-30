namespace SalaoBeleza.Models;

public class PedidoServico
{
    public int PedidoId { get; set; }

    public int ServicoId { get; set; }

    public Pedido Pedido { get; set; } = null!;

    public Servico Servico { get; set; } = null!;
}