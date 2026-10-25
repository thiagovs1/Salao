
namespace SalaoBeleza.Models;

public class AvaliacaoCurtida
{
    public int Id { get; set; }
    public int AvaliacaoId { get; set; }
    public int ClienteId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public Avaliacao Avaliacao { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
}
