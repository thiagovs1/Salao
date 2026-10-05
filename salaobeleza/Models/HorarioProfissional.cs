namespace SalaoBeleza.Models;

public class HorarioProfissional
{
    public int Id { get; set; }

    public int ProfissionalId { get; set; }

    public Profissional? Profissional { get; set; }

    public DayOfWeek DiaSemana { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFim { get; set; }
}