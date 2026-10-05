using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CombosController : ControllerBase
{
    private readonly AppDbContext _context;

    public CombosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCombos()
    {
        var combos = await _context.Combos
            .Include(c => c.ComboServicos)
                .ThenInclude(cs => cs.Servico)
            .Select(c => new
            {
                id = c.Id,
                nome = c.Nome,
                preco = c.Preco,
                servicos = c.ComboServicos
                    .Select(cs => new
                    {
                        id = cs.Servico.Id,
                        nome = cs.Servico.Nome,
                        preco = cs.Servico.Preco,
                        duracaoMinutos = cs.Servico.DuracaoMinutos
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(combos);
    }
}