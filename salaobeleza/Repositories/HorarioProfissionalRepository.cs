using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

public class HorarioProfissionalRepository : IHorarioProfissionalRepository
{
    private readonly AppDbContext _context;

    public HorarioProfissionalRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<HorarioProfissional>> BuscarPorProfissionalEDia(
        int profissionalId,
        DayOfWeek diaSemana)
    {
        return await _context.HorariosProfissionais
            .Where(h =>
                h.ProfissionalId == profissionalId &&
                h.DiaSemana == diaSemana)
            .OrderBy(h => h.HoraInicio)
            .ToListAsync();
    }
}