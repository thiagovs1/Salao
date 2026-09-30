using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

public interface IClienteRepository
{
    Task<List<Cliente>> ListarTodos();

    Task<Cliente?> BuscarPorId(int id);

    Task<Cliente?> BuscarPorEmail(string email);

    Task<Cliente> Adicionar(Cliente cliente);

    Task Atualizar(Cliente cliente);

    Task Remover(Cliente cliente);
}