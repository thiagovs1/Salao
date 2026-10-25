using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.DTOs;
using SalaoBeleza.Services;
using System.Text.Json;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgendamentosController : ControllerBase
{
    private readonly AgendamentoService _service;
    private const string EmailAdmin = "admistrador@salaobeleza.com";

    public AgendamentosController(AgendamentoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        bool administrador = EhAdministrador();
        bool funcionario = HttpContext.Session.GetString("TipoUsuarioFuncionario") == "Funcionario"
            && HttpContext.Session.GetInt32("ProfissionalId").HasValue;

        if (!administrador && !funcionario)
            return Unauthorized(new { mensagem = "Faça login como administrador ou profissional para consultar os agendamentos." });

        var lista = await _service.Listar();
        if (funcionario)
        {
            int profissionalId = HttpContext.Session.GetInt32("ProfissionalId")!.Value;
            // A lista geral só é usada pelo painel administrativo. Funcionários consultam sua agenda própria.
            return Forbid();
        }

        return Ok(lista);
    }

    [HttpGet("meus")]
    public async Task<IActionResult> MeusAgendamentos()
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null)
            return Unauthorized(new { mensagem = "Faça login para consultar seus agendamentos." });
        return Ok(await _service.ListarPorCliente(clienteId.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] AgendamentoCriarDTO dto)
    {
        if (dto == null)
            return BadRequest(new { mensagem = "Os dados do agendamento não foram enviados." });

        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null)
            return Unauthorized(new { mensagem = "Faça login para agendar um atendimento." });

        try
        {
            var jsonProfissional = HttpContext.Session.GetString("PROFISSIONAL_SELECIONADO");
            var profissional = string.IsNullOrWhiteSpace(jsonProfissional)
                ? null
                : JsonSerializer.Deserialize<ProfissionalSelecionado>(jsonProfissional);

            var dtoFinal = dto with { ClienteId = clienteId.Value };
            var criado = await _service.Criar(dtoFinal);
            if (profissional != null)
                criado = criado with { NomeProfissional = profissional.Nome };

            HttpContext.Session.Remove("SELECAO_SERVICOS");
            return Ok(criado);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpGet("horarios-disponiveis")]
    public async Task<IActionResult> HorariosDisponiveis(
        int profissionalId, DateTime data, int? servicoId = null,
        int? comboId = null, string? servicoIds = null)
    {
        try
        {
            var ids = new List<int>();
            if (!string.IsNullOrWhiteSpace(servicoIds))
            {
                ids = servicoIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x, out var id) ? id : 0)
                    .Where(id => id > 0).Distinct().ToList();
            }

            if (ids.Count == 0 && servicoId.GetValueOrDefault() > 0)
                ids.Add(servicoId!.Value);

            var horarios = await _service.BuscarHorariosDisponiveis(profissionalId, ids, comboId, data);
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
            var trabalha = await _service.VerificarDiaTrabalho(profissionalId, diaSemana);
            return Ok(new { trabalha });
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPut("{id}/atendido")]
    public async Task<IActionResult> MarcarComoAtendido(int id)
    {
        bool administrador = EhAdministrador();
        int? profissionalId = HttpContext.Session.GetInt32("ProfissionalId");
        bool funcionario = HttpContext.Session.GetString("TipoUsuarioFuncionario") == "Funcionario"
            && profissionalId.HasValue;

        if (!administrador && !funcionario)
            return Unauthorized(new { mensagem = "Acesso permitido somente à equipe do salão." });

        try
        {
            bool atualizado = await _service.MarcarComoAtendido(id, administrador ? null : profissionalId);
            if (!atualizado)
                return NotFound(new { mensagem = "Agendamento não encontrado." });

            return Ok(new { mensagem = "Atendimento marcado como realizado.", status = "Atendido" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPut("{id}/pagamento")]
    public async Task<IActionResult> RegistrarPagamento(int id, [FromBody] FormaPagamentoDTO dto)
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null)
            return Unauthorized(new { mensagem = "Faça login para continuar." });

        if (dto == null || (dto.FormaPagamento != "Pix" && dto.FormaPagamento != "No salão"))
            return BadRequest(new { mensagem = "Selecione uma forma de pagamento válida." });

        try
        {
            var salvo = await _service.RegistrarFormaPagamento(id, clienteId.Value, dto.FormaPagamento);
            if (!salvo)
                return NotFound(new { mensagem = "Agendamento não encontrado." });

            HttpContext.Session.Remove("PROFISSIONAL_SELECIONADO");
            return Ok(new { mensagem = "Forma de pagamento registrada.", formaPagamento = dto.FormaPagamento });
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

        try
        {
            var ok = await _service.Cancelar(id, clienteId.Value);
            if (!ok)
                return NotFound(new { mensagem = "Agendamento não encontrado." });
            return Ok(new { mensagem = "Agendamento cancelado com sucesso." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(int id)
    {
        if (!EhAdministrador())
            return Unauthorized(new { mensagem = "Acesso restrito ao administrador." });

        var ok = await _service.Remover(id);
        if (!ok)
            return NotFound(new { mensagem = "Agendamento não encontrado." });
        return NoContent();
    }

    private bool EhAdministrador()
    {
        return HttpContext.Session.GetString("AdministradorAutenticado") == "true"
            && HttpContext.Session.GetString("TipoUsuarioAdmin") == "Administrador"
            && HttpContext.Session.GetString("EmailAdmin") == EmailAdmin;
    }
}

public record FormaPagamentoDTO(string FormaPagamento);
