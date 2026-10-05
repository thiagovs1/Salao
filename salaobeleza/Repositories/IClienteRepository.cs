using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

// Contrato: define o que o repositorio faz, sem dizer como.
public interface IClienteRepository
{
    Task<List<Cliente>> ListarTodos();
    Task<Cliente?> BuscarPorId(int id);
    Task<Cliente> Adicionar(Cliente cliente);
    Task Atualizar(Cliente cliente);
    Task Remover(Cliente cliente);
    Task<Cliente?> BuscarPorTelefone(string telefone);
    Task<Cliente?> BuscarPorEmail(string email);
    
}
