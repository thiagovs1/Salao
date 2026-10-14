
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfissionaisController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfissionaisController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/profissionais
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var profissionais = await _context.Profissionais
            .Where(p => p.Ativo)
            .Select(p => new
            {
                id = p.Id,
                nome = p.Nome,
                profissao = p.Profissao,
                imagem = p.Foto
            })
            .ToListAsync();

        return Ok(profissionais);
    }

    // GET api/profissionais/painel
    [HttpGet("painel")]
    public async Task<IActionResult> Painel()
    {
        var profissionalId =
            HttpContext.Session.GetInt32("ProfissionalId");

        if (profissionalId == null)
        {
            return Unauthorized(new
            {
                mensagem = "Profissional não está logada."
            });
        }

        var profissional = await _context.Profissionais
            .FirstOrDefaultAsync(p =>
                p.Id == profissionalId.Value && p.Ativo);

        if (profissional == null)
        {
            HttpContext.Session.Remove("ProfissionalId");

            return NotFound(new
            {
                mensagem = "Profissional não encontrada ou inativa."
            });
        }

        var horarios = await _context.HorariosProfissionais
            .Where(h => h.ProfissionalId == profissional.Id)
            .OrderBy(h => h.DiaSemana)
            .ThenBy(h => h.HoraInicio)
            .Select(h => new
            {
                diaSemana = h.DiaSemana,
                horaInicio = h.HoraInicio.ToString(),
                horaFim = h.HoraFim.ToString()
            })
            .ToListAsync();

        var agendamentos = await _context.Agendamentos
            .Include(a => a.Cliente)
            .Include(a => a.Servico)
            .Include(a => a.Combo)
                .ThenInclude(c => c.ComboServicos)
                    .ThenInclude(cs => cs.Servico)
            .Where(a =>
                a.ProfissionalId == profissional.Id &&
                a.Status != "Cancelado")
            .OrderBy(a => a.DataHora)
            .ToListAsync();

        var listaAgendamentos = agendamentos.Select(a =>
        {
            var nomeAtendimento = a.Combo != null
                ? a.Combo.Nome
                : a.Servico?.Nome ?? "Atendimento";

            var preco = a.Combo != null
                ? a.Combo.Preco
                : a.Servico?.Preco ?? 0;

            var duracao = a.Servico != null
                ? a.Servico.DuracaoMinutos
                : a.Combo != null
                    ? a.Combo.Nome.Equals(
                        "Belleza completa",
                        StringComparison.OrdinalIgnoreCase)
                        ? 240
                        : a.Combo.ComboServicos.Sum(
                            cs => cs.Servico.DuracaoMinutos)
                    : 0;

            return new
            {
                id = a.Id,
                dataHora = a.DataHora,
                cliente = a.Cliente?.Nome ?? "Cliente",
                atendimento = nomeAtendimento,
                tipo = a.Combo != null ? "Combo" : "Serviço",
                preco,
                duracaoMinutos = duracao,
                status = a.Status
            };
        }).ToList();

        return Ok(new
        {
            profissional = new
            {
                id = profissional.Id,
                nome = profissional.Nome,
                profissao = profissional.Profissao,
                imagem = profissional.Foto
            },
            horariosTrabalho = horarios,
            agendamentos = listaAgendamentos
        });
    }
}
