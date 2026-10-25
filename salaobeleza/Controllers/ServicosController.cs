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

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _service.Listar());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var servico = await _service.BuscarPorId(id);

        if (servico == null)
            return NotFound();

        return Ok(servico);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] ServicoCriarDTO dto
    )
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
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] ServicoCriarDTO dto
    )
    {
        try
        {
            var atualizado = await _service.Atualizar(id, dto);

            if (atualizado == null)
                return NotFound();

            return Ok(atualizado);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(int id)
    {
        try
        {
            var removido = await _service.Remover(id);

            if (!removido)
                return NotFound();

            return NoContent();
        }
        catch (Exception)
        {
            return Conflict(new
            {
                mensagem = "Não foi possível excluir o serviço. Ele pode estar vinculado a agendamentos ou combos."
            });
        }
    }
}