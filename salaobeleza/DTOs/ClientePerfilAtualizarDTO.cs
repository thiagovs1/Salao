using System.ComponentModel.DataAnnotations;

namespace SalaoBeleza.DTOs;

public class ClientePerfilAtualizarDTO
{
    [Required]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Telefone { get; set; } = string.Empty;

    [StringLength(14)]
    public string? CPF { get; set; }

    [StringLength(255)]
    public string? Endereco { get; set; }
}