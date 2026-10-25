namespace SalaoBeleza.Models;

public class ServicoProfissional
{
    public int ServicoId { get; set; }
    public Servico? Servico { get; set; }

    public int ProfissionalId { get; set; }
    public Profissional? Profissional { get; set; }
}