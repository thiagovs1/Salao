using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.DTOs;
using SalaoBeleza.Services;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgendamentosController : ControllerBase
{
    private readonly AgendamentoService _service;

    public AgendamentosController(
        AgendamentoService service)
    {
        _service = service;
    }

    // GET api/agendamentos
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var agendamentos =
            await _service.Listar();

        return Ok(agendamentos);
    }

    // GET api/agendamentos/meus
    [HttpGet("meus")]
    public async Task<IActionResult> MeusAgendamentos()
    {
        var clienteId =
            HttpContext.Session.GetInt32(
                "ClienteId"
            );

        if (clienteId == null)
        {
            return Unauthorized(new
            {
                mensagem =
                    "Cliente não está logado."
            });
        }

        var agendamentos =
            await _service.ListarPorCliente(
                clienteId.Value
            );

        return Ok(agendamentos);
    }

    // POST api/agendamentos
    [HttpPost]
    public async Task<IActionResult> Criar(
        AgendamentoCriarDTO dto)
    {
        try
        {
            var clienteId =
                HttpContext.Session.GetInt32(
                    "ClienteId"
                );

            if (clienteId == null)
            {
                return Unauthorized(new
                {
                    mensagem =
                        "Cliente não está logado."
                });
            }

            var agendamentoDTO =
                new AgendamentoCriarDTO(
                    dto.DataHora,
                    clienteId.Value,
                    dto.ServicoId,
                    dto.ComboId,
                    dto.ProfissionalId
                );

            var criado =
                await _service.Criar(
                    agendamentoDTO
                );


            // =====================================================
            // AGENDAMENTO FINALIZADO COM SUCESSO
            // LIMPAR A SELEÇÃO DA SESSION
            // =====================================================

            HttpContext.Session.Remove(
                "SELECAO_SERVICOS"
            );


            HttpContext.Session.Remove(
                "PROFISSIONAL_SELECIONADO"
            );


            return Ok(criado);
        }
        catch (Exception ex)
        {
            return BadRequest(
                ex.Message
            );
        }
    }

    // GET api/agendamentos/horarios-disponiveis
    [HttpGet("horarios-disponiveis")]
    public async Task<IActionResult> HorariosDisponiveis(
        int profissionalId,
        int? servicoId,
        int? comboId,
        DateTime data)
    {
        try
        {
            var horarios =
                await _service.BuscarHorariosDisponiveis(
                    profissionalId,
                    servicoId,
                    comboId,
                    data
                );

            return Ok(horarios);
        }
        catch (Exception ex)
        {
            return BadRequest(
                ex.Message
            );
        }
    }

    // GET api/agendamentos/dia-trabalho
    [HttpGet("dia-trabalho")]
    public async Task<IActionResult> DiaTrabalho(
        int profissionalId,
        int diaSemana)
    {
        try
        {
            var trabalha =
                await _service.VerificarDiaTrabalho(
                    profissionalId,
                    diaSemana
                );

            return Ok(new
            {
                trabalha = trabalha
            });
        }
        catch (Exception ex)
        {
            return BadRequest(
                ex.Message
            );
        }
    }

    // PUT api/agendamentos/5/cancelar
    [HttpPut("{id}/cancelar")]
    public async Task<IActionResult> Cancelar(
        int id)
    {
        var clienteId =
            HttpContext.Session.GetInt32(
                "ClienteId"
            );

        if (clienteId == null)
        {
            return Unauthorized(new
            {
                mensagem =
                    "Cliente não está logado."
            });
        }

        var ok =
            await _service.Cancelar(
                id,
                clienteId.Value
            );

        if (!ok)
        {
            return NotFound(new
            {
                mensagem =
                    "Agendamento não encontrado."
            });
        }

        return Ok(new
        {
            mensagem =
                "Agendamento cancelado com sucesso."
        });
    }

    // DELETE api/agendamentos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(
        int id)
    {
        var ok =
            await _service.Remover(id);

        if (!ok)
            return NotFound();

        return NoContent();
    }
}