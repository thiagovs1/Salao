using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.DTOs;
using SalaoBeleza.Services;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _service;

    public ClientesController(ClienteService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _service.Listar());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var cliente = await _service.BuscarPorId(id);

        if (cliente == null)
            return NotFound();

        return Ok(cliente);
    }

    [HttpGet("meu-perfil")]
    public async Task<IActionResult> MeuPerfil()
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");

        if (clienteId == null)
            return Unauthorized(new { mensagem = "Faça login para continuar." });

        var cliente = await _service.BuscarPorId(clienteId.Value);

        if (cliente == null)
            return NotFound(new { mensagem = "Cliente não encontrado." });

        return Ok(new
        {
            id = cliente.Id,
            nome = cliente.Nome,
            email = cliente.Email,
            telefone = cliente.Telefone
        });
    }

    [HttpPost]
    public async Task<IActionResult> Criar(ClienteCriarDTO dto)
    {
        try
        {
            var criado = await _service.Criar(dto);

            return CreatedAtAction(
                nameof(BuscarPorId),
                new { id = criado.Id },
                criado
            );
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(int id, ClienteCriarDTO dto)
    {
        var ok = await _service.Atualizar(id, dto);

        if (!ok)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(int id)
    {
        var ok = await _service.Remover(id);

        if (!ok)
            return NotFound();

        return NoContent();
    }
}