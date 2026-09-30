using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

public class AgendamentoRepository : IAgendamentoRepository
{
    private readonly AppDbContext _context;

    public AgendamentoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Agendamento>> ListarTodos()
    {
        return await _context.Agendamentos
            .Include(a => a.Cliente)
            .Include(a => a.Profissional)
            .Include(a => a.AgendamentoServicos)
                .ThenInclude(ags => ags.Servico)
            .ToListAsync();
    }

    public async Task<Agendamento?> BuscarPorId(int id)
    {
        return await _context.Agendamentos
            .Include(a => a.Cliente)
            .Include(a => a.Profissional)
            .Include(a => a.AgendamentoServicos)
                .ThenInclude(ags => ags.Servico)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Agendamento> Adicionar(Agendamento agendamento)
    {
        _context.Agendamentos.Add(agendamento);

        await _context.SaveChangesAsync();

        return agendamento;
    }

    public async Task Remover(Agendamento agendamento)
    {
        _context.Agendamentos.Remove(agendamento);

        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExisteConflito(
        int profissionalId,
        DateTime inicio,
        DateTime fim)
    {
        var agendamentos = await _context.Agendamentos
            .Where(a =>
                a.ProfissionalId == profissionalId &&
                a.Status != "Cancelado")
            .ToListAsync();

        foreach (var agendamento in agendamentos)
        {
            var inicioExistente = agendamento.DataHora;

            var duracao = await _context.AgendamentoServicos
                .Where(ags => ags.AgendamentoId == agendamento.Id)
                .Include(ags => ags.Servico)
                .Select(ags => (int?)ags.Servico.DuracaoMinutos)
                .SumAsync() ?? 0;

            var fimExistente = inicioExistente.AddMinutes(duracao);

            if (inicio < fimExistente && fim > inicioExistente)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<List<HorarioProfissional>> ListarHorariosProfissional(
        int profissionalId)
    {
        return await _context.HorariosProfissionais
            .Where(h => h.ProfissionalId == profissionalId)
            .OrderBy(h => h.DiaSemana)
            .ThenBy(h => h.HoraInicio)
            .ToListAsync();
    }

    public async Task<List<Agendamento>> ListarAgendamentosDoDia(
        int profissionalId,
        DateTime data)
    {
        var inicio = data.Date;
        var fim = inicio.AddDays(1);

        return await _context.Agendamentos
            .Where(a =>
                a.ProfissionalId == profissionalId &&
                a.DataHora >= inicio &&
                a.DataHora < fim &&
                a.Status != "Cancelado")
            .Include(a => a.AgendamentoServicos)
                .ThenInclude(ags => ags.Servico)
            .ToListAsync();
    }
}