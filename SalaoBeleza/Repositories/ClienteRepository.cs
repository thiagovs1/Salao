using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Cliente>> ListarTodos()
    {
        return await _context.Clientes.ToListAsync();
    }

    public async Task<Cliente?> BuscarPorId(int id)
    {
        return await _context.Clientes.FindAsync(id);
    }

    public async Task<Cliente?> BuscarPorEmail(string email)
    {
        return await _context.Clientes
            .FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<Cliente> Adicionar(Cliente cliente)
    {
        _context.Clientes.Add(cliente);

        await _context.SaveChangesAsync();

        return cliente;
    }

    public async Task Atualizar(Cliente cliente)
    {
        _context.Clientes.Update(cliente);

        await _context.SaveChangesAsync();
    }

    public async Task Remover(Cliente cliente)
    {
        _context.Clientes.Remove(cliente);

        await _context.SaveChangesAsync();
    }
}