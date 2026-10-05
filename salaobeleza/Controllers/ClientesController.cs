using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.DTOs;
using SalaoBeleza.Services;

namespace SalaoBeleza.Controllers;

// Porta de entrada da API. Recebe as requisicoes HTTP.
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _service;

    public ClientesController(ClienteService service)
    {
        _service = service;
    }

    // GET api/clientes
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var clientes = await _service.Listar();
        return Ok(clientes);
    }

    // GET api/clientes/5
    [HttpGet("{id}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var cliente = await _service.BuscarPorId(id);
        if (cliente == null)
            return NotFound();

        return Ok(cliente);
    }

    // POST api/clientes
    [HttpPost]
    public async Task<IActionResult> Criar(ClienteCriarDTO dto)
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

    // PUT api/clientes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(int id, ClienteCriarDTO dto)
    {
        var ok = await _service.Atualizar(id, dto);
        if (!ok)
            return NotFound();

        return NoContent();
    }

    // DELETE api/clientes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(int id)
    {
        var ok = await _service.Remover(id);
        if (!ok)
            return NotFound();

        return NoContent();
    }
}
