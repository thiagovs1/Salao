using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario> Adicionar(Usuario usuario);
    Task<Usuario?> BuscarPorEmail(string email);
    Task<bool> AlterarSenha(string email, string novaSenha);
}