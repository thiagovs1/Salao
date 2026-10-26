
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinanceiroController : ControllerBase
{
    private readonly AppDbContext _context;

    public FinanceiroController(AppDbContext context)
    {
        _context = context;
    }

    private bool AdministradorAutenticado()
    {
        return HttpContext.Session.GetString("AdministradorAutenticado") == "true"
            || HttpContext.Session.GetString("TipoUsuarioAdmin") == "Administrador"
            || !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("EmailAdmin"));
    }

    [HttpGet]
    public async Task<IActionResult> Resumo(
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        [FromQuery] string? tipo)
    {
        if (!AdministradorAutenticado())
            return Unauthorized(new { mensagem = "Faça login como administrador." });

        var dataInicio = (inicio ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var dataFim = (fim ?? DateTime.Today).Date;

        if (dataInicio > dataFim)
            return BadRequest(new { mensagem = "A data inicial não pode ser posterior à data final." });

        var limite = dataFim.AddDays(1);

        var agendamentos = await _context.Agendamentos
            .AsNoTracking()
            .Include(a => a.Servico)
            .Include(a => a.AgendamentoServicos)
                .ThenInclude(x => x.Servico)
            .Include(a => a.Combo)
            .Where(a =>
                a.Status == "Atendido" &&
                a.DataHora >= dataInicio &&
                a.DataHora < limite)
            .OrderByDescending(a => a.DataHora)
            .ToListAsync();

        var despesas = await _context.Despesas
            .AsNoTracking()
            .Where(d => d.Data >= dataInicio && d.Data < limite)
            .OrderByDescending(d => d.Data)
            .ToListAsync();

        var movimentacoes = new List<MovimentacaoFinanceira>();

        foreach (var agendamento in agendamentos)
        {
            decimal valor = agendamento.Combo?.Preco ?? 0m;

            var servicos = agendamento.AgendamentoServicos
                .Where(x => x.Servico != null)
                .Select(x => x.Servico!)
                .ToList();

            if (servicos.Count > 0)
            {
                valor += servicos.Sum(s => s.Preco);
            }
            else if (agendamento.Servico != null && agendamento.ComboId == null)
            {
                valor += agendamento.Servico.Preco;
            }

            if (valor <= 0)
                continue;

            var nomes = servicos.Select(s => s.Nome).ToList();

            if (nomes.Count == 0 && agendamento.Servico != null)
                nomes.Add(agendamento.Servico.Nome);

            if (agendamento.Combo != null)
                nomes.Insert(0, agendamento.Combo.Nome);

            movimentacoes.Add(new MovimentacaoFinanceira
            {
                Id = $"R-{agendamento.Id}",
                Data = agendamento.DataHora,
                Descricao = nomes.Count > 0
                    ? string.Join(" + ", nomes)
                    : $"Atendimento #{agendamento.Id}",
                Categoria = "Serviços",
                Tipo = "receita",
                Valor = valor
            });
        }

        foreach (var despesa in despesas)
        {
            movimentacoes.Add(new MovimentacaoFinanceira
            {
                Id = $"D-{despesa.Id}",
                Data = despesa.Data,
                Descricao = despesa.Descricao,
                Categoria = despesa.Categoria,
                Tipo = "despesa",
                Valor = despesa.Valor
            });
        }

        movimentacoes = movimentacoes
            .OrderByDescending(m => m.Data)
            .ToList();

        var receitas = movimentacoes
            .Where(m => m.Tipo == "receita")
            .Sum(m => m.Valor);

        var totalDespesas = movimentacoes
            .Where(m => m.Tipo == "despesa")
            .Sum(m => m.Valor);

        var filtradas = movimentacoes.AsEnumerable();

        if (string.Equals(tipo, "receita", StringComparison.OrdinalIgnoreCase))
            filtradas = filtradas.Where(m => m.Tipo == "receita");
        else if (string.Equals(tipo, "despesa", StringComparison.OrdinalIgnoreCase))
            filtradas = filtradas.Where(m => m.Tipo == "despesa");

        var dias = Enumerable.Range(0, (dataFim - dataInicio).Days + 1)
            .Select(i => dataInicio.AddDays(i))
            .ToList();

        var grafico = dias.Select(dia =>
        {
            var doDia = movimentacoes.Where(m => m.Data.Date == dia).ToList();

            return new
            {
                data = dia.ToString("yyyy-MM-dd"),
                receitas = doDia.Where(m => m.Tipo == "receita").Sum(m => m.Valor),
                despesas = doDia.Where(m => m.Tipo == "despesa").Sum(m => m.Valor)
            };
        }).ToList();

        var despesasPorCategoria = despesas
            .GroupBy(d => d.Categoria)
            .Select(g => new
            {
                categoria = g.Key,
                valor = g.Sum(d => d.Valor)
            })
            .OrderByDescending(g => g.valor)
            .ToList();

        return Ok(new
        {
            inicio = dataInicio.ToString("yyyy-MM-dd"),
            fim = dataFim.ToString("yyyy-MM-dd"),
            receitas,
            despesas = totalDespesas,
            saldo = receitas - totalDespesas,
            faturamento = receitas,
            quantidadeAtendimentos = agendamentos.Count,
            movimentacoes = filtradas.Select(m => new
            {
                m.Id,
                data = m.Data.ToString("yyyy-MM-dd"),
                descricao = m.Descricao,
                categoria = m.Categoria,
                tipo = m.Tipo,
                valor = m.Valor
            }).ToList(),
            grafico,
            despesasPorCategoria
        });
    }

    [HttpPost("despesas")]
    public async Task<IActionResult> CriarDespesa([FromBody] DespesaDTO dto)
    {
        if (!AdministradorAutenticado())
            return Unauthorized(new { mensagem = "Faça login como administrador." });

        if (string.IsNullOrWhiteSpace(dto.Descricao))
            return BadRequest(new { mensagem = "Informe a descrição da despesa." });

        if (string.IsNullOrWhiteSpace(dto.Categoria))
            return BadRequest(new { mensagem = "Selecione a categoria da despesa." });

        if (dto.Valor <= 0)
            return BadRequest(new { mensagem = "O valor deve ser maior que zero." });

        if (dto.Data == default)
            return BadRequest(new { mensagem = "Informe a data da despesa." });

        var despesa = new Despesa
        {
            Descricao = dto.Descricao.Trim(),
            Categoria = dto.Categoria.Trim(),
            Valor = dto.Valor,
            Data = dto.Data
        };

        _context.Despesas.Add(despesa);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Despesa cadastrada com sucesso.",
            id = despesa.Id
        });
    }
}

public class DespesaDTO
{
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Outros";
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
}

public class MovimentacaoFinanceira
{
    public string Id { get; set; } = string.Empty;
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
