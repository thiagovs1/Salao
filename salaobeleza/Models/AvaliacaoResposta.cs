
using System.ComponentModel.DataAnnotations;

namespace SalaoBeleza.Models;

public class AvaliacaoResposta
{
    public int Id { get; set; }
    public int AvaliacaoId { get; set; }
    public int ClienteId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Resposta { get; set; } = string.Empty;

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public Avaliacao Avaliacao { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
}
