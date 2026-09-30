using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

public class ServicoRepository : IServicoRepository
{
    private readonly AppDbContext _context;

    public ServicoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Servico>> ListarTodos()
    {
        return await _context.Servicos.ToListAsync();
    }

    public async Task<Servico?> BuscarPorId(int id)
    {
        return await _context.Servicos.FindAsync(id);
    }

    public async Task<Servico> Adicionar(Servico servico)
    {
        _context.Servicos.Add(servico);
        await _context.SaveChangesAsync();
        return servico;
    }

    public async Task Atualizar(Servico servico)
    {
        _context.Servicos.Update(servico);
        await _context.SaveChangesAsync();
    }

    public async Task Remover(Servico servico)
    {
        _context.Servicos.Remove(servico);
        await _context.SaveChangesAsync();
    }
}
