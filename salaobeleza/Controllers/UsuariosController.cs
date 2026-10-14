
using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.DTOs;
using SalaoBeleza.Services;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _service;

    public UsuariosController(UsuarioService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(UsuarioCriarDTO dto)
    {
        try
        {
            var usuario = await _service.Criar(dto);

            return Ok(new
            {
                id = usuario.Id,
                nome = usuario.Nome,
                email = usuario.Email,
                tipoUsuario = usuario.TipoUsuario
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UsuarioLoginDTO dto)
    {
        var usuario = await _service.Login(dto);

        if (usuario == null)
        {
            return Unauthorized(new
            {
                mensagem = "E-mail ou senha incorretos."
            });
        }

        HttpContext.Session.Clear();

        if (string.Equals(
            usuario.TipoUsuario,
            "Funcionario",
            StringComparison.OrdinalIgnoreCase))
        {
            if (usuario.ProfissionalId == null)
            {
                return Unauthorized(new
                {
                    mensagem = "Esta conta não está vinculada a uma profissional."
                });
            }

            HttpContext.Session.SetInt32(
                "ProfissionalId",
                usuario.ProfissionalId.Value
            );

            return Ok(new
            {
                id = usuario.Id,
                nome = usuario.Nome,
                email = usuario.Email,
                tipoUsuario = "Funcionario",
                profissionalId = usuario.ProfissionalId,
                redirecionamento = "/funcionarios/html/painel_profissional.html"
            });
        }

        var cliente = await _service.BuscarClientePorEmail(usuario.Email);

        if (cliente == null)
        {
            return Unauthorized(new
            {
                mensagem = "Cliente não encontrado para esta conta."
            });
        }

        HttpContext.Session.SetInt32("ClienteId", cliente.Id);

        return Ok(new
        {
            id = usuario.Id,
            nome = usuario.Nome,
            email = usuario.Email,
            tipoUsuario = "Cliente",
            redirecionamento = "/html/tela_inicial.html"
        });
    }

    [HttpPost("recuperar-senha")]
    public async Task<IActionResult> RecuperarSenha(
        UsuarioRecuperarSenhaDTO dto)
    {
        try
        {
            var alterou = await _service.RecuperarSenha(dto);

            if (!alterou)
            {
                return BadRequest(new
                {
                    mensagem = "Não foi possível alterar a senha."
                });
            }

            return Ok(new
            {
                mensagem = "Senha alterada com sucesso."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return Ok(new
        {
            mensagem = "Sessão encerrada com sucesso."
        });
    }
}
