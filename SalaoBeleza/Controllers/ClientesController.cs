
using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.DTOs;
using SalaoBeleza.Services;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClienteController : ControllerBase
{
    private readonly ClienteService _service;

    public ClienteController(ClienteService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var clientes = await _service.Listar();

        return Ok(clientes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var cliente = await _service.BuscarPorId(id);

        if (cliente == null)
            return NotFound();

        return Ok(cliente);
    }

    [HttpPost]
public async Task<IActionResult> Criar([FromBody] ClienteDTO dto)
{
    try
    {
        var cliente = await _service.Criar(dto);

        return Ok(cliente);
    }
    catch (Exception ex)
    {
        return BadRequest(new
        {
            erro = ex.Message,
            detalhe = ex.InnerException?.Message
        });
    }
}
    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] ClienteDTO dto)
    {
        var atualizado = await _service.Atualizar(id, dto);

        if (!atualizado)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(int id)
    {
        var removido = await _service.Remover(id);

        if (!removido)
            return NotFound();

        return NoContent();
    }
}
