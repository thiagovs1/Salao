namespace SalaoBeleza.DTOs;

public record ClienteDTO(
    string Nome,
    string Telefone,
    string Email,
    string Senha,
    string? Cpf
);

public record ClienteRespostaDTO(
    int Id,
    string Nome,
    string Telefone,
    string Email
);