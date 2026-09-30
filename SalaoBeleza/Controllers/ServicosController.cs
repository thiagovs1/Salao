using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.DTOs;
using SalaoBeleza.Services;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicosController : ControllerBase
{
    private readonly ServicoService _service;

    public ServicosController(ServicoService service)
    {
        _service = service;
    }

    // GET api/servicos
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var servicos = await _service.Listar();
        return Ok(servicos);
    }

    // GET api/servicos/5
    [HttpGet("{id}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var servico = await _service.BuscarPorId(id);
        if (servico == null)
            return NotFound();

        return Ok(servico);
    }

    // POST api/servicos
    [HttpPost]
    public async Task<IActionResult> Criar(ServicoCriarDTO dto)
    {
        try
        {
            var criado = await _service.Criar(dto);
            return CreatedAtAction(nameof(BuscarPorId), new { id = criado.Id }, criado);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // DELETE api/servicos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(int id)
    {
        var ok = await _service.Remover(id);
        if (!ok)
            return NotFound();

        return NoContent();
    }
}
