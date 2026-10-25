
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalaoBeleza.Models;

[Table("Profissionais")]
public class Profissional
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string Profissao { get; set; } = "";

    [MaxLength(255)]
    public string Foto { get; set; } = "";

    public bool Ativo { get; set; } = true;

    [MaxLength(14)]
    public string? CPF { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(255)]
    public string? Endereco { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxaAtendimento { get; set; } = 0.00m;

    public List<Agendamento> Agendamentos { get; set; } = new();

    public List<HorarioProfissional> HorariosProfissionais { get; set; } = new();

    public List<ServicoProfissional> ServicosProfissionais { get; set; } = new();
}
