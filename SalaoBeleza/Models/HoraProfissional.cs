namespace SalaoBeleza.Models;

public class HorarioProfissional
{
    public int Id { get; set; }

    public int ProfissionalId { get; set; }

    public string DiaSemana { get; set; } = string.Empty;

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFim { get; set; }

    public Profissional Profissional { get; set; } = null!;
}