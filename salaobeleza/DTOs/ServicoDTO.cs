namespace SalaoBeleza.DTOs;

public record ServicoCriarDTO(
    string Nome,
    decimal Preco,
    int DuracaoMinutos,
    List<int>? ProfissionalIds = null
);

public record ProfissionalServicoDTO(
    int Id,
    string Nome
);

public record ServicoRespostaDTO(
    int Id,
    string Nome,
    decimal Preco,
    int DuracaoMinutos,
    List<ProfissionalServicoDTO> Profissionais
);