namespace SalaoBeleza.DTOs;

public record ServicoCriarDTO(string Nome, decimal Preco, int DuracaoMinutos);

public record ServicoRespostaDTO(int Id, string Nome, decimal Preco, int DuracaoMinutos);
