using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

public interface IHorarioProfissionalRepository
{
    Task<List<HorarioProfissional>> BuscarPorProfissionalEDia(
        int profissionalId,
        DayOfWeek diaSemana);
}