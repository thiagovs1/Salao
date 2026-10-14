namespace SalaoBeleza.Models;

public class Agendamento
{
    public int Id { get; set; }
    public DateTime DataHora { get; set; }
    public string Status { get; set; } = "Aberto";
    public string? FormaPagamento { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int? ServicoId { get; set; }
    public Servico? Servico { get; set; }

    public int? ComboId { get; set; }
    public Combo? Combo { get; set; }

    public int ProfissionalId { get; set; }
    public Profissional? Profissional { get; set; }
}