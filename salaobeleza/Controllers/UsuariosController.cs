
using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.DTOs;
using SalaoBeleza.Services;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _service;

    private const string EmailAdmin = "admistrador@salaobeleza.com";
    private const string SenhaAdmin = "12345";

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
        // LOGIN DO ADMINISTRADOR
        if (string.Equals(
                dto.Email?.Trim(),
                EmailAdmin,
                StringComparison.OrdinalIgnoreCase)
            && dto.Senha == SenhaAdmin)
        {
            HttpContext.Session.SetString(
                "AdministradorAutenticado", "true");

            HttpContext.Session.SetString(
                "TipoUsuarioAdmin", "Administrador");

            HttpContext.Session.SetString(
                "EmailAdmin", EmailAdmin);

            HttpContext.Session.SetString(
                "TipoUsuario", "Administrador");

            return Ok(new
            {
                nome = "Administrador",
                email = EmailAdmin,
                tipoUsuario = "Administrador",
                redirecionamento =
                    "/administrador/html/Painel_administrador.html"
            });
        }

        // LOGIN NORMAL
        var usuario = await _service.Login(dto);

        if (usuario == null)
        {
            return Unauthorized(new
            {
                mensagem = "E-mail ou senha incorretos."
            });
        }

        // LOGIN DE FUNCIONÁRIO
        if (string.Equals(
                usuario.TipoUsuario,
                "Funcionario",
                StringComparison.OrdinalIgnoreCase))
        {
            if (usuario.ProfissionalId == null)
            {
                return Unauthorized(new
                {
                    mensagem =
                        "Esta conta não está vinculada a uma profissional."
                });
            }

            HttpContext.Session.SetInt32(
                "ProfissionalId",
                usuario.ProfissionalId.Value);

            HttpContext.Session.SetString(
                "TipoUsuarioFuncionario", "Funcionario");

            HttpContext.Session.SetString(
                "TipoUsuario", "Funcionario");

            return Ok(new
            {
                id = usuario.Id,
                nome = usuario.Nome,
                email = usuario.Email,
                tipoUsuario = "Funcionario",
                profissionalId = usuario.ProfissionalId,
                redirecionamento =
                    "/funcionarios/html/painel_profissional.html"
            });
        }

        // LOGIN DE CLIENTE
        var cliente =
            await _service.BuscarClientePorEmail(usuario.Email);

        if (cliente == null)
        {
            return Unauthorized(new
            {
                mensagem = "Cliente não encontrado para esta conta."
            });
        }

        HttpContext.Session.SetInt32("ClienteId", cliente.Id);

        HttpContext.Session.SetString(
            "TipoUsuarioCliente", "Cliente");

        HttpContext.Session.SetString(
            "TipoUsuario", "Cliente");

        return Ok(new
        {
            id = usuario.Id,
            nome = usuario.Nome,
            email = usuario.Email,
            tipoUsuario = "Cliente",
            redirecionamento = "/html/tela_inicial.html"
        });
    }

    [HttpGet("verificar-admin")]
    public IActionResult VerificarAdmin()
    {
        bool autenticado =
            HttpContext.Session.GetString(
                "AdministradorAutenticado") == "true"
            && HttpContext.Session.GetString(
                "TipoUsuarioAdmin") == "Administrador"
            && HttpContext.Session.GetString(
                "EmailAdmin") == EmailAdmin;

        if (!autenticado)
        {
            return Unauthorized(new
            {
                mensagem = "Acesso restrito ao administrador."
            });
        }

        return Ok(new
        {
            autenticado = true,
            tipoUsuario = "Administrador"
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

    // O parâmetro identifica qual conta deve ser desconectada.
    // Exemplos: /api/Usuarios/logout?tipoUsuario=Administrador
    //           /api/Usuarios/logout?tipoUsuario=Cliente
    //           /api/Usuarios/logout?tipoUsuario=Funcionario
    [HttpPost("logout")]
    public IActionResult Logout(
        [FromQuery] string? tipoUsuario)
    {
        // Compatibilidade com páginas que ainda não enviam o parâmetro.
        if (string.IsNullOrWhiteSpace(tipoUsuario))
        {
            var referer = Request.Headers.Referer.ToString();

            if (referer.Contains(
                "/administrador/",
                StringComparison.OrdinalIgnoreCase))
            {
                tipoUsuario = "Administrador";
            }
            else if (referer.Contains(
                "/funcionarios/",
                StringComparison.OrdinalIgnoreCase))
            {
                tipoUsuario = "Funcionario";
            }
            else
            {
                tipoUsuario = "Cliente";
            }
        }

        if (string.Equals(
            tipoUsuario,
            "Administrador",
            StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.Remove("AdministradorAutenticado");
            HttpContext.Session.Remove("TipoUsuarioAdmin");
            HttpContext.Session.Remove("EmailAdmin");

            if (HttpContext.Session.GetString("TipoUsuario")
                == "Administrador")
            {
                HttpContext.Session.Remove("TipoUsuario");
            }
        }
        else if (string.Equals(
            tipoUsuario,
            "Funcionario",
            StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.Remove("ProfissionalId");
            HttpContext.Session.Remove("TipoUsuarioFuncionario");

            if (HttpContext.Session.GetString("TipoUsuario")
                == "Funcionario")
            {
                HttpContext.Session.Remove("TipoUsuario");
            }
        }
        else if (string.Equals(
            tipoUsuario,
            "Cliente",
            StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.Remove("ClienteId");
            HttpContext.Session.Remove("TipoUsuarioCliente");

            if (HttpContext.Session.GetString("TipoUsuario")
                == "Cliente")
            {
                HttpContext.Session.Remove("TipoUsuario");
            }
        }
        else
        {
            return BadRequest(new
            {
                mensagem = "Tipo de conta inválido."
            });
        }

        return Ok(new
        {
            mensagem = "Sessão encerrada com sucesso."
        });
    }
}
