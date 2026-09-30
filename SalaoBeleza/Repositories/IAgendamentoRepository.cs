using SalaoBeleza.Models;

namespace SalaoBeleza.Repositories;

public interface IAgendamentoRepository
{
    Task<List<Agendamento>> ListarTodos();

    Task<Agendamento?> BuscarPorId(int id);

    Task<Agendamento> Adicionar(Agendamento agendamento);

    Task Remover(Agendamento agendamento);

    Task<bool> ExisteConflito(
        int profissionalId,
        DateTime inicio,
        DateTime fim
    );

    Task<List<HorarioProfissional>> ListarHorariosProfissional(
        int profissionalId
    );

    Task<List<Agendamento>> ListarAgendamentosDoDia(
        int profissionalId,
        DateTime data
    );
}