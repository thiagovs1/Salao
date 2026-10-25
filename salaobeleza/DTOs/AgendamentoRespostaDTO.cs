
namespace SalaoBeleza.DTOs;

public record AgendamentoRespostaDTO(
    int Id,
    DateTime DataHora,
    string NomeCliente,
    string NomeServico,
    decimal Preco,
    string Status,
    string NomeProfissional = ""
);
