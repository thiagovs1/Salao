
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
        return await _context.Servicos
            .AsNoTracking()
            .Include(s => s.Profissionais)
                .ThenInclude(sp => sp.Profissional)
            .OrderBy(s => s.Nome)
            .ToListAsync();
    }

    public async Task<Servico?> BuscarPorId(int id)
    {
        return await _context.Servicos
            .Include(s => s.Profissionais)
                .ThenInclude(sp => sp.Profissional)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Profissional>> BuscarProfissionaisAtivosPorIds(List<int> ids)
    {
        if (ids.Count == 0)
            return new List<Profissional>();

        return await _context.Profissionais
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.Ativo)
            .ToListAsync();
    }

    public async Task<Servico> Adicionar(Servico servico)
    {
        _context.Servicos.Add(servico);
        await _context.SaveChangesAsync();

        return servico;
    }

    public async Task Atualizar(Servico servico)
    {
        // Os campos e vínculos já foram alterados pelo ServicoService.
        // Não consultar, remover nem adicionar vínculos novamente aqui.
        await _context.SaveChangesAsync();
    }

    public async Task Remover(Servico servico)
    {
        _context.Servicos.Remove(servico);
        await _context.SaveChangesAsync();
    }
}
