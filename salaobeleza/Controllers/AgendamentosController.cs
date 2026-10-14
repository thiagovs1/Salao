using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.DTOs;
using SalaoBeleza.Services;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        return Ok(await _service.Listar());
    }

    [HttpGet("meus")]
    public async Task<IActionResult> MeusAgendamentos()
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");

        if (clienteId == null)
            return Unauthorized(new { mensagem = "Cliente não está logado." });

        return Ok(await _service.ListarPorCliente(clienteId.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Criar(AgendamentoCriarDTO dto)
    {
        try
        {
            var clienteId = HttpContext.Session.GetInt32("ClienteId");

            if (clienteId == null)
                return Unauthorized(new { mensagem = "Cliente não está logado." });

            var agendamentoDTO = new AgendamentoCriarDTO(
                dto.DataHora,
                clienteId.Value,
                dto.ServicoId,
                dto.ComboId,
                dto.ProfissionalId
            );

            var criado = await _service.Criar(agendamentoDTO);

            HttpContext.Session.Remove("SELECAO_SERVICOS");
            HttpContext.Session.Remove("PROFISSIONAL_SELECIONADO");

            return Ok(criado);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpGet("horarios-disponiveis")]
    public async Task<IActionResult> HorariosDisponiveis(
        int profissionalId,
        int? servicoId,
        int? comboId,
        DateTime data)
    {
        try
        {
            var horarios = await _service.BuscarHorariosDisponiveis(
                profissionalId,
                servicoId,
                comboId,
                data
            );

            return Ok(horarios);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpGet("dia-trabalho")]
    public async Task<IActionResult> DiaTrabalho(int profissionalId, int diaSemana)
    {
        try
        {
            var trabalha = await _service.VerificarDiaTrabalho(
                profissionalId,
                diaSemana
            );

            return Ok(new { trabalha });
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPut("{id}/pagamento")]
    public async Task<IActionResult> RegistrarPagamento(
        int id,
        [FromBody] FormaPagamentoDTO dto)
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");

        if (clienteId == null)
            return Unauthorized(new { mensagem = "Faça login para continuar." });

        if (dto == null ||
            (dto.FormaPagamento != "Pix" &&
             dto.FormaPagamento != "No salão"))
        {
            return BadRequest(new { mensagem = "Forma de pagamento inválida." });
        }

        try
        {
            var salvo = await _service.RegistrarFormaPagamento(
                id,
                clienteId.Value,
                dto.FormaPagamento
            );

            if (!salvo)
                return NotFound(new { mensagem = "Agendamento não encontrado." });

            return Ok(new
            {
                mensagem = "Forma de pagamento registrada.",
                formaPagamento = dto.FormaPagamento
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPut("{id}/cancelar")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");

        if (clienteId == null)
            return Unauthorized(new { mensagem = "Cliente não está logado." });

        var ok = await _service.Cancelar(id, clienteId.Value);

        if (!ok)
            return NotFound(new { mensagem = "Agendamento não encontrado." });

        return Ok(new { mensagem = "Agendamento cancelado com sucesso." });
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

public record FormaPagamentoDTO(string FormaPagamento);