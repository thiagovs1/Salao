namespace SalaoBeleza.Models;

// Liga um cliente, profissional e serviço ou combo
// a uma data e hora.
public class Agendamento
{
    public int Id { get; set; }

    public DateTime DataHora { get; set; }

    public string Status { get; set; } = "Aberto";

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    // Quando o agendamento for de um serviço.
    public int? ServicoId { get; set; }
    public Servico? Servico { get; set; }

    // Quando o agendamento for de um combo.
    public int? ComboId { get; set; }
    public Combo? Combo { get; set; }

    public int ProfissionalId { get; set; }
    public Profissional? Profissional { get; set; }
}