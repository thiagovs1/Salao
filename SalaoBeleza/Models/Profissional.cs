namespace SalaoBeleza.Models;

public class Profissional
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Especialidade { get; set; } = string.Empty;

    public decimal Avaliacao { get; set; }

    public List<HorarioProfissional> Horarios { get; set; } = new();

    public List<Agendamento> Agendamentos { get; set; } = new();
}