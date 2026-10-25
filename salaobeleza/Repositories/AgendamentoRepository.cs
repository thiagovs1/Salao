
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

    private IQueryable<Agendamento> ConsultaCompleta()
    {
        return _context.Agendamentos
            .Include(a => a.Cliente)
            .Include(a => a.Servico)
            .Include(a => a.AgendamentoServicos)
                .ThenInclude(ag => ag.Servico)
            .Include(a => a.Combo)
                .ThenInclude(c => c!.ComboServicos)
                    .ThenInclude(cs => cs.Servico)
            .Include(a => a.Profissional);
    }

    public async Task<List<Agendamento>> ListarTodos()
    {
        return await ConsultaCompleta()
            .ToListAsync();
    }

    public async Task<List<Agendamento>> ListarPorCliente(int clienteId)
    {
        return await ConsultaCompleta()
            .Where(a => a.ClienteId == clienteId)
            .OrderByDescending(a => a.DataHora)
            .ToListAsync();
    }

    public async Task<Agendamento?> BuscarPorId(int id)
    {
        return await ConsultaCompleta()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Agendamento>> BuscarPorProfissionalEData(
        int profissionalId,
        DateTime data)
    {
        return await ConsultaCompleta()
            .Where(a =>
                a.ProfissionalId == profissionalId &&
                a.DataHora.Date == data.Date &&
                a.Status != "Cancelado")
            .OrderBy(a => a.DataHora)
            .ToListAsync();
    }

    public async Task<List<Agendamento>> ListarPorProfissionalEPeriodo(
        int profissionalId,
        DateTime inicio,
        DateTime fim)
    {
        return await ConsultaCompleta()
            .Where(a =>
                a.ProfissionalId == profissionalId &&
                a.DataHora >= inicio &&
                a.DataHora < fim)
            .OrderBy(a => a.DataHora)
            .ToListAsync();
    }

    public async Task<Combo?> BuscarComboPorId(int id)
    {
        return await _context.Combos
            .Include(c => c.ComboServicos)
                .ThenInclude(cs => cs.Servico)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Agendamento> Adicionar(Agendamento agendamento)
    {
        _context.Agendamentos.Add(agendamento);

        await _context.SaveChangesAsync();

        return agendamento;
    }

    public async Task Atualizar(Agendamento agendamento)
    {
        _context.Agendamentos.Update(agendamento);

        await _context.SaveChangesAsync();
    }

    public async Task Remover(Agendamento agendamento)
    {
        _context.Agendamentos.Remove(agendamento);

        await _context.SaveChangesAsync();
    }
}
