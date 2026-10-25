
namespace SalaoBeleza.DTOs;

public record AgendamentoCriarDTO
{
    public DateTime DataHora { get; init; }
    public int ClienteId { get; init; }
    public int ProfissionalId { get; init; }
    public int? ServicoId { get; init; }
    public List<int> ServicoIds { get; init; } = new();
    public int? ComboId { get; init; }
}
