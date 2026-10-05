namespace SalaoBeleza.DTOs;

// Dados necessários para criar um agendamento.
public record AgendamentoCriarDTO(

    DateTime DataHora,

    int ClienteId,

    int? ServicoId,

    int? ComboId,

    int ProfissionalId

);

// Dados devolvidos pela API.
public record AgendamentoRespostaDTO(

    int Id,

    DateTime DataHora,

    string NomeCliente,

    string NomeServico,

    decimal Preco,

    string Status

);