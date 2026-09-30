using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.DTOs;
using SalaoBeleza.Services;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/agendamento")]
[Route("api/agendamentos")]
public class AgendamentosController : ControllerBase
{
    private readonly AgendamentoService _service;

    public AgendamentosController(AgendamentoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var agendamentos = await _service.Listar();

        return Ok(agendamentos);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] AgendamentoCriarDTO dto)
    {
        try
        {
            var cookieCliente = Request.Cookies["clienteId"];

            if (!int.TryParse(cookieCliente, out var clienteId))
            {
                return Unauthorized(new
                {
                    mensagem = "Cliente nao esta logado."
                });
            }

            var dtoComCliente = dto with
            {
                ClienteId = clienteId
            };

            var criado = await _service.Criar(dtoComCliente);

            return Ok(criado);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }

    [HttpGet("calendario")]
    public async Task<IActionResult> Calendario(
        int profissionalId,
        int ano,
        int mes)
    {
        try
        {
            var resultado = await _service.Calendario(
                profissionalId,
                ano,
                mes
            );

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }

    [HttpGet("horarios")]
    public async Task<IActionResult> Horarios(
        int profissionalId,
        int servicoId,
        string data)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return BadRequest(new
                {
                    mensagem = "Data não informada."
                });
            }

            var partes = data.Split('-');

            if (partes.Length != 3 ||
                !int.TryParse(partes[0], out var ano) ||
                !int.TryParse(partes[1], out var mes) ||
                !int.TryParse(partes[2], out var dia))
            {
                return BadRequest(new
                {
                    mensagem = "Formato de data inválido."
                });
            }

            var dataConvertida = new DateTime(
                ano,
                mes,
                dia
            );

            var resultado = await _service.Horarios(
                profissionalId,
                servicoId,
                dataConvertida
            );

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message,
                erro = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("horarios-multiplos")]
    public async Task<IActionResult> HorariosMultiplos(
        int profissionalId,
        [FromQuery] List<int> servicoIds,
        string data)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return BadRequest(new
                {
                    mensagem = "Data não informada."
                });
            }

            var partes = data.Split('-');

            if (partes.Length != 3 ||
                !int.TryParse(partes[0], out var ano) ||
                !int.TryParse(partes[1], out var mes) ||
                !int.TryParse(partes[2], out var dia))
            {
                return BadRequest(new
                {
                    mensagem = "Formato de data inválido."
                });
            }

            var dataConvertida = new DateTime(
                ano,
                mes,
                dia
            );

            var resultado = await _service.Horarios(
                profissionalId,
                servicoIds,
                dataConvertida
            );

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message,
                erro = ex.InnerException?.Message
            });
        }
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