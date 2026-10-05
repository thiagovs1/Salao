using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

public interface IServicoRepository
{
    Task<List<Servico>> ListarTodos();
    Task<Servico?> BuscarPorId(int id);
    Task<Servico> Adicionar(Servico servico);
    Task Atualizar(Servico servico);
    Task Remover(Servico servico);
}
