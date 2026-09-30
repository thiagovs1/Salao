namespace SalaoBeleza.Models;

public class Agendamento
{
    public int Id { get; set; }

    public DateTime DataHora { get; set; }

    public int ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    public int ProfissionalId { get; set; }

    public Profissional? Profissional { get; set; }

    public string Status { get; set; } = "Pendente";

    public List<AgendamentoServico> AgendamentoServicos { get; set; } = new();
}