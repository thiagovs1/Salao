
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalaoBeleza.Models;

public class Avaliacao
{
    public int Id { get; set; }
    public int ClienteId { get; set; }

    [Range(1, 5)]
    public int Nota { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Comentario { get; set; } = string.Empty;

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    [ForeignKey(nameof(ClienteId))]
    public Cliente Cliente { get; set; } = null!;

    public ICollection<AvaliacaoCurtida> Curtidas { get; set; }
        = new List<AvaliacaoCurtida>();

    public ICollection<AvaliacaoResposta> Respostas { get; set; }
        = new List<AvaliacaoResposta>();
}
