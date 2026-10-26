
namespace SalaoBeleza.Models;

public class Despesa
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Outros";
    public decimal Valor { get; set; }
    public DateTime Data { get; set; } = DateTime.Now;
}
