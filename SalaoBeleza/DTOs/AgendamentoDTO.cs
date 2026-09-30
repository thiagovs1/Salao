namespace SalaoBeleza.DTOs;

public record AgendamentoCriarDTO(
    DateTime DataHora,
    int ClienteId,
    int ProfissionalId,
    List<int> ServicoIds
);

public record AgendamentoRespostaDTO(
    int Id,
    DateTime DataHora,
    string NomeCliente,
    string NomeProfissional,
    List<string> Servicos,
    decimal PrecoTotal,
    string Status
);

public record CalendarioDiaDTO(
    string Data,
    bool Disponivel
);

public record HorarioDisponivelDTO(
    string Inicio,
    string Fim
);