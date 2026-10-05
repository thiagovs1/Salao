namespace SalaoBeleza.DTOs;

// Dados que chegam da API para criar ou editar um cliente.
public record ClienteCriarDTO(string Nome, string Telefone, string Email);

// Dados que a API devolve.
public record ClienteRespostaDTO(int Id, string Nome, string Telefone, string Email);
