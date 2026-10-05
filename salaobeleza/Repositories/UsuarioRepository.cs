using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
private readonly AppDbContext _context;


public UsuarioRepository(AppDbContext context)
{
    _context = context;
}

public async Task<Usuario> Adicionar(Usuario usuario)
{
    _context.Usuarios.Add(usuario);
    await _context.SaveChangesAsync();

    return usuario;
}

public async Task<Usuario?> BuscarPorEmail(string email)
{
    return await _context.Usuarios
        .FirstOrDefaultAsync(u => u.Email == email);
}

public async Task<bool> AlterarSenha(string email, string novaSenha)
{
    var usuario = await BuscarPorEmail(email);

    if (usuario == null)
        return false;

    usuario.Senha = novaSenha;

    await _context.SaveChangesAsync();

    return true;
}


}
