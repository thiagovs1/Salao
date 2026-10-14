
using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.Repositories;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/agenda-profissional")]
public class AgendaProfissionalController : ControllerBase
{
    private readonly IAgendamentoRepository _agendamentoRepository;

    public AgendaProfissionalController(
        IAgendamentoRepository agendamentoRepository)
    {
        _agendamentoRepository = agendamentoRepository;
    }

    [HttpGet("dia")]
    public async Task<IActionResult> ListarDia(
        [FromQuery] DateTime? data)
    {
        var profissionalId = HttpContext.Session.GetInt32("ProfissionalId");

        if (profissionalId == null)
        {
            return Unauthorized(new
            {
                mensagem = "Faça login como profissional para acessar a agenda."
            });
        }

        var dataConsulta = (data ?? DateTime.Today).Date;

        var agendamentos =
            await _agendamentoRepository.BuscarPorProfissionalEData(
                profissionalId.Value,
                dataConsulta
            );

        var resultado = agendamentos.Select(a => new
        {
            id = a.Id,
            dataHora = a.DataHora,
            cliente = a.Cliente?.Nome ?? "Cliente não identificado",
            telefone = a.Cliente?.Telefone ?? "",
            servico = a.Servico?.Nome ?? a.Combo?.Nome ?? "Atendimento",
            servicosCombo = a.Combo?.ComboServicos?
                .Where(cs => cs.Servico != null)
                .Select(cs => cs.Servico!.Nome)
                .ToList() ?? new List<string>(),
            duracaoMinutos = a.Servico?.DuracaoMinutos
                ?? (a.Combo == null ? 0
                    : a.Combo.Nome == "Belleza completa" ? 240
                    : a.Combo.ComboServicos?
                        .Where(cs => cs.Servico != null)
                        .Sum(cs => cs.Servico!.DuracaoMinutos) ?? 0),
            preco = a.Servico?.Preco ?? a.Combo?.Preco ?? 0,
            status = a.Status,
            profissional = a.Profissional?.Nome ?? ""
        }).ToList();

        return Ok(new
        {
            data = dataConsulta.ToString("yyyy-MM-dd"),
            total = resultado.Count,
            agendamentos = resultado
        });
    }

    [HttpGet("mes")]
    public async Task<IActionResult> ListarMes(
        [FromQuery] int? ano,
        [FromQuery] int? mes)
    {
        var profissionalId = HttpContext.Session.GetInt32("ProfissionalId");

        if (profissionalId == null)
        {
            return Unauthorized(new
            {
                mensagem = "Faça login como profissional para acessar a agenda."
            });
        }

        var hoje = DateTime.Today;
        var anoConsulta = ano ?? hoje.Year;
        var mesConsulta = mes ?? hoje.Month;

        if (anoConsulta < 2000 || anoConsulta > 2100 ||
            mesConsulta < 1 || mesConsulta > 12)
        {
            return BadRequest(new
            {
                mensagem = "Ano ou mês inválido."
            });
        }

        var inicio = new DateTime(anoConsulta, mesConsulta, 1);
        var fim = inicio.AddMonths(1);

        var agendamentos =
            await _agendamentoRepository.ListarPorProfissionalEPeriodo(
                profissionalId.Value,
                inicio,
                fim
            );

        var resultado = agendamentos.Select(a => new
        {
            id = a.Id,
            dataHora = a.DataHora,
            cliente = a.Cliente?.Nome ?? "Cliente não identificado",
            telefone = a.Cliente?.Telefone ?? "",
            servico = a.Servico?.Nome ?? a.Combo?.Nome ?? "Atendimento",
            duracaoMinutos = a.Servico?.DuracaoMinutos
                ?? (a.Combo == null ? 0
                    : a.Combo.Nome == "Belleza completa" ? 240
                    : a.Combo.ComboServicos?
                        .Where(cs => cs.Servico != null)
                        .Sum(cs => cs.Servico!.DuracaoMinutos) ?? 0),
            preco = a.Servico?.Preco ?? a.Combo?.Preco ?? 0,
            status = a.Status
        }).ToList();

        return Ok(new
        {
            ano = anoConsulta,
            mes = mesConsulta,
            total = resultado.Count,
            agendamentos = resultado
        });
    }

    [HttpPost("sair")]
    public IActionResult Sair()
    {
        HttpContext.Session.Clear();

        return Ok(new
        {
            mensagem = "Sessão encerrada."
        });
    }
}
