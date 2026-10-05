using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

public interface IAgendamentoRepository
{
    Task<List<Agendamento>> ListarTodos();

    Task<List<Agendamento>> ListarPorCliente(
        int clienteId
    );

    Task<Agendamento?> BuscarPorId(
        int id
    );

    Task<List<Agendamento>> BuscarPorProfissionalEData(
        int profissionalId,
        DateTime data
    );

    Task<Combo?> BuscarComboPorId(
        int id
    );

    Task<Agendamento> Adicionar(
        Agendamento agendamento
    );

    Task Atualizar(
        Agendamento agendamento
    );

    Task Remover(
        Agendamento agendamento
    );
}