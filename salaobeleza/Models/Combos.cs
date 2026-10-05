using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalaoBeleza.Models
{
    [Table("Combo")]
    public class Combo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Preco { get; set; }

        public ICollection<ComboServico> ComboServicos { get; set; }
            = new List<ComboServico>();
    }
}