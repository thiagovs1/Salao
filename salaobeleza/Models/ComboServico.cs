using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalaoBeleza.Models
{
    [Table("ComboServico")]
    public class ComboServico
    {
        [Key, Column(Order = 0)]
        public int ComboId { get; set; }

        [Key, Column(Order = 1)]
        public int ServicoId { get; set; }

        [ForeignKey("ComboId")]
        public Combo Combo { get; set; } = null!;

        [ForeignKey("ServicoId")]
        public Servico Servico { get; set; } = null!;
    }
}