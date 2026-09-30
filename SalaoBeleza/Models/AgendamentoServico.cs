namespace SalaoBeleza.Models;

public class AgendamentoServico
{
    public int AgendamentoId { get; set; }

    public int ServicoId { get; set; }

    public Agendamento Agendamento { get; set; } = null!;

    public Servico Servico { get; set; } = null!;
}