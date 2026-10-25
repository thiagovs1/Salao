using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgendamentosAdministracaoController : ControllerBase
{
    private readonly AppDbContext _context;

    public AgendamentosAdministracaoController(AppDbContext context)
    {
        _context = context;
    }

    private bool AdministradorAutenticado()
    {
        return HttpContext.Session.GetString("AdministradorAutenticado") == "true"
            || HttpContext.Session.GetString("TipoUsuarioAdmin") == "Administrador"
            || !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("EmailAdmin"));
    }

    private async Task<Combo?> BuscarCombo(int id)
    {
        return await _context.Combos
            .Include(c => c.ComboServicos)
                .ThenInclude(cs => cs.Servico)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    private static int DuracaoCombo(Combo combo)
    {
        if (string.Equals(combo.Nome, "Belleza completa", StringComparison.OrdinalIgnoreCase))
            return 240;

        return combo.ComboServicos?.Sum(cs => cs.Servico?.DuracaoMinutos ?? 0) ?? 0;
    }

    private static int DuracaoAgendamento(Agendamento agendamento)
    {
        int duracaoExtras = agendamento.AgendamentoServicos?
            .Sum(x => x.Servico?.DuracaoMinutos ?? 0) ?? 0;

        if (duracaoExtras == 0 && agendamento.Servico != null)
            duracaoExtras = agendamento.Servico.DuracaoMinutos;

        return duracaoExtras + (agendamento.Combo == null ? 0 : DuracaoCombo(agendamento.Combo));
    }

    private static bool ServicosDuplicadosNoCombo(Combo combo, List<int> servicoIds)
    {
        var idsCombo = combo.ComboServicos?
            .Select(cs => cs.ServicoId)
            .ToHashSet() ?? new HashSet<int>();

        return servicoIds.Any(idsCombo.Contains);
    }

    private async Task<List<Agendamento>> BuscarAgendamentosDoDia(int profissionalId, DateTime data)
    {
        return await _context.Agendamentos
            .Include(a => a.Servico)
            .Include(a => a.AgendamentoServicos)
                .ThenInclude(x => x.Servico)
            .Include(a => a.Combo)
                .ThenInclude(c => c!.ComboServicos)
                    .ThenInclude(cs => cs.Servico)
            .Where(a =>
                a.ProfissionalId == profissionalId &&
                a.DataHora >= data.Date &&
                a.DataHora < data.Date.AddDays(1) &&
                a.Status != "Cancelado")
            .ToListAsync();
    }

    private async Task<(Combo? Combo, List<Servico> Servicos, string? Erro)> ValidarSelecao(
        List<int> ids,
        int? comboId)
    {
        var servicos = await _context.Servicos
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        if (servicos.Count != ids.Count)
            return (null, servicos, "Um ou mais serviços selecionados não foram encontrados.");

        Combo? combo = null;

        if (comboId.HasValue)
        {
            if (comboId.Value <= 0)
                return (null, servicos, "O combo selecionado é inválido.");

            combo = await BuscarCombo(comboId.Value);

            if (combo == null)
                return (null, servicos, "O combo selecionado não foi encontrado.");

            if (combo.ComboServicos == null || combo.ComboServicos.Count == 0)
                return (null, servicos, "O combo não possui serviços cadastrados.");

            if (ServicosDuplicadosNoCombo(combo, ids))
            {
                var duplicados = combo.ComboServicos
                    .Where(cs => ids.Contains(cs.ServicoId))
                    .Select(cs => cs.Servico?.Nome)
                    .Where(nome => !string.IsNullOrWhiteSpace(nome))
                    .Distinct()
                    .ToList();

                string mensagem = duplicados.Count > 0
                    ? $"O combo \"{combo.Nome}\" já inclui: {string.Join(", ", duplicados)}."
                    : "Um ou mais serviços já fazem parte do combo.";

                return (combo, servicos, mensagem);
            }
        }

        if (ids.Count == 0 && combo == null)
            return (null, servicos, "Selecione pelo menos um serviço ou combo.");

        return (combo, servicos, null);
    }

    [HttpGet("combos")]
    public async Task<IActionResult> ListarCombos()
    {
        if (!AdministradorAutenticado())
            return Unauthorized(new { mensagem = "Faça login como administrador." });

        var combos = await _context.Combos
            .Include(c => c.ComboServicos)
            .ToListAsync();

        return Ok(combos.Select(c => new
        {
            id = c.Id,
            nome = c.Nome,
            preco = c.Preco,
            duracaoMinutos = DuracaoCombo(c),
            servicoIds = c.ComboServicos.Select(cs => cs.ServicoId).ToList()
        }));
    }

    [HttpGet("horarios-disponiveis")]
    public async Task<IActionResult> HorariosDisponiveis(
        [FromQuery] int profissionalId,
        [FromQuery] DateTime data,
        [FromQuery] List<int>? servicoIds,
        [FromQuery] int? comboId)
    {
        if (!AdministradorAutenticado())
            return Unauthorized(new { mensagem = "Faça login como administrador." });

        if (profissionalId <= 0 || data == default)
            return BadRequest(new { mensagem = "Informe a profissional e a data." });

        var ids = (servicoIds ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var validacao = await ValidarSelecao(ids, comboId);

        if (validacao.Erro != null)
            return BadRequest(new { mensagem = validacao.Erro });

        int duracao = validacao.Servicos.Sum(s => s.DuracaoMinutos)
            + (validacao.Combo == null ? 0 : DuracaoCombo(validacao.Combo));

        if (duracao <= 0)
            return BadRequest(new { mensagem = "Não foi possível calcular a duração do atendimento." });

        var expediente = await _context.HorariosProfissionais
            .Where(h => h.ProfissionalId == profissionalId && h.DiaSemana == data.DayOfWeek)
            .ToListAsync();

        if (expediente.Count == 0)
            return Ok(new List<object>());

        var existentes = await BuscarAgendamentosDoDia(profissionalId, data);
        var agora = DateTime.Now;
        var resultado = new List<string>();

        foreach (var horario in expediente)
        {
            var inicioExpediente = data.Date.Add(horario.HoraInicio);
            var fimExpediente = data.Date.Add(horario.HoraFim);

            for (var inicio = inicioExpediente;
                 inicio.AddMinutes(duracao) <= fimExpediente;
                 inicio = inicio.AddMinutes(30))
            {
                if (inicio <= agora)
                    continue;

                var fim = inicio.AddMinutes(duracao);

                bool ocupado = existentes.Any(a =>
                {
                    int duracaoExistente = DuracaoAgendamento(a);
                    if (duracaoExistente <= 0)
                        duracaoExistente = 30;

                    var inicioExistente = a.DataHora;
                    var fimExistente = inicioExistente.AddMinutes(duracaoExistente);

                    return inicio < fimExistente && fim > inicioExistente;
                });

                if (!ocupado)
                    resultado.Add(inicio.ToString("HH:mm"));
            }
        }

        return Ok(resultado
            .Distinct()
            .OrderBy(h => h)
            .Select(h => new { horario = h, disponivel = true }));
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] AgendamentoAdministracaoDTO dto)
    {
        if (!AdministradorAutenticado())
            return Unauthorized(new { mensagem = "Faça login como administrador para cadastrar agendamentos." });

        if (dto.ClienteId <= 0 || dto.ProfissionalId <= 0)
            return BadRequest(new { mensagem = "Selecione o cliente e a profissional." });

        if (dto.DataHora == default || dto.DataHora < DateTime.Now)
            return BadRequest(new { mensagem = "Informe uma data e horário futuros válidos." });

        var ids = (dto.ServicoIds ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (!await _context.Clientes.AnyAsync(c => c.Id == dto.ClienteId))
            return BadRequest(new { mensagem = "Cliente não encontrado." });

        if (!await _context.Profissionais.AnyAsync(p => p.Id == dto.ProfissionalId))
            return BadRequest(new { mensagem = "Profissional não encontrada." });

        var validacao = await ValidarSelecao(ids, dto.ComboId);

        if (validacao.Erro != null)
            return BadRequest(new { mensagem = validacao.Erro });

        var combo = validacao.Combo;
        var servicos = validacao.Servicos;

        int duracao = servicos.Sum(s => s.DuracaoMinutos)
            + (combo == null ? 0 : DuracaoCombo(combo));

        if (duracao <= 0)
            return BadRequest(new { mensagem = "Não foi possível calcular a duração do atendimento." });

        var expediente = await _context.HorariosProfissionais
            .Where(h =>
                h.ProfissionalId == dto.ProfissionalId &&
                h.DiaSemana == dto.DataHora.DayOfWeek)
            .ToListAsync();

        var inicioNovo = dto.DataHora;
        var fimNovo = inicioNovo.AddMinutes(duracao);

        bool dentroExpediente = expediente.Any(h =>
        {
            var inicio = dto.DataHora.Date.Add(h.HoraInicio);
            var fim = dto.DataHora.Date.Add(h.HoraFim);
            return inicioNovo >= inicio && fimNovo <= fim;
        });

        if (!dentroExpediente)
            return BadRequest(new
            {
                mensagem = "O horário escolhido não comporta a duração do atendimento dentro do expediente."
            });

        var existentes = await BuscarAgendamentosDoDia(dto.ProfissionalId, dto.DataHora);

        bool conflito = existentes.Any(a =>
        {
            int duracaoExistente = DuracaoAgendamento(a);
            if (duracaoExistente <= 0)
                duracaoExistente = 30;

            var inicio = a.DataHora;
            var fim = inicio.AddMinutes(duracaoExistente);

            return inicioNovo < fim && fimNovo > inicio;
        });

        if (conflito)
            return Conflict(new
            {
                mensagem = "A profissional já possui um atendimento nesse intervalo. Escolha outro horário."
            });

        var novo = new Agendamento
        {
            ClienteId = dto.ClienteId,
            ProfissionalId = dto.ProfissionalId,
            DataHora = dto.DataHora,
            ComboId = combo?.Id,
            ServicoId = combo == null && servicos.Count == 1
                ? servicos[0].Id
                : null,
            Status = dto.Status == "Confirmado" ? "Confirmado" : "Aberto",
            AgendamentoServicos = servicos.Select(s => new AgendamentoServico
            {
                ServicoId = s.Id
            }).ToList()
        };

        _context.Agendamentos.Add(novo);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new
            {
                mensagem = "Não foi possível salvar o agendamento no banco. Verifique as tabelas e os relacionamentos."
            });
        }

        return Ok(new
        {
            mensagem = "Agendamento cadastrado com sucesso.",
            id = novo.Id
        });
    }
}

public class AgendamentoAdministracaoDTO
{
    public int ClienteId { get; set; }
    public int ProfissionalId { get; set; }
    public DateTime DataHora { get; set; }
    public List<int> ServicoIds { get; set; } = new();
    public int? ComboId { get; set; }
    public string Status { get; set; } = "Aberto";
}