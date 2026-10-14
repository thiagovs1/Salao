
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalaoBeleza.Models;

[Table("Usuario")]
public class Usuario
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [Column("Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [Column("Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("Senha")]
    public string Senha { get; set; } = string.Empty;

    [Required]
    [Column("TipoUsuario")]
    public string TipoUsuario { get; set; } = "Cliente";

    [Column("ProfissionalId")]
    public int? ProfissionalId { get; set; }

    public Profissional? Profissional { get; set; }
}
