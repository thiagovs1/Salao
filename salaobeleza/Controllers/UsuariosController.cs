
using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.DTOs;
using SalaoBeleza.Services;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _service;
    private readonly IConfiguration _configuration;

    private const string EmailAdmin =
        "Administrador@adm.Belleza.com";

    private const string DominioAdmin =
        "@adm.belleza.com";

    public UsuariosController(
        UsuarioService service,
        IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    private bool AdminAutenticado()
    {
        return HttpContext.Session.GetString(
            "TipoUsuarioAdmin") == "Administrador"
            && !string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("EmailAdmin"));
    }

    private bool AdminPrincipalAutenticado()
    {
        return HttpContext.Session.GetString(
            "AdministradorPrincipalAutenticado") == "true"
            && string.Equals(
                HttpContext.Session.GetString("EmailAdmin"),
                EmailAdmin,
                StringComparison.OrdinalIgnoreCase);
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

    // Somente o administrador principal pode criar administradores.
    [HttpPost("criar-administrador")]
    public async Task<IActionResult> CriarAdministrador(
        UsuarioCriarDTO dto)
    {
        if (!AdminPrincipalAutenticado())
        {
            return Unauthorized(new
            {
                mensagem = "Somente o administrador principal pode cadastrar administradores."
            });
        }

        try
        {
            var usuario = await _service.CriarAdministrador(dto);

            return Ok(new
            {
                id = usuario.Id,
                nome = usuario.Nome,
                email = usuario.Email,
                tipoUsuario = usuario.TipoUsuario,
                mensagem = "Administrador cadastrado com sucesso."
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
        if (string.IsNullOrWhiteSpace(dto.Email)
            || string.IsNullOrWhiteSpace(dto.Senha))
        {
            return Unauthorized(new
            {
                mensagem = "E-mail ou senha incorretos."
            });
        }

        var email = dto.Email.Trim();

        var senhaAdmin = _configuration["Administrador:Senha"];

        // LOGIN DO ADMINISTRADOR PRINCIPAL
        if (string.Equals(
                email,
                EmailAdmin,
                StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(senhaAdmin)
                || dto.Senha != senhaAdmin)
            {
                return Unauthorized(new
                {
                    mensagem = "E-mail ou senha incorretos."
                });
            }

            HttpContext.Session.Clear();

            HttpContext.Session.SetString(
                "AdministradorAutenticado", "true");

            HttpContext.Session.SetString(
                "AdministradorPrincipalAutenticado", "true");

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
                principal = true,
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

        HttpContext.Session.Clear();

        // LOGIN DE ADMINISTRADOR SECUNDÁRIO
        if (string.Equals(
            usuario.TipoUsuario,
            "Administrador",
            StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.SetString(
                "AdministradorAutenticado", "true");

            HttpContext.Session.SetString(
                "TipoUsuarioAdmin", "Administrador");

            HttpContext.Session.SetString(
                "EmailAdmin", usuario.Email);

            HttpContext.Session.SetString(
                "TipoUsuario", "Administrador");

            return Ok(new
            {
                id = usuario.Id,
                nome = usuario.Nome,
                email = usuario.Email,
                tipoUsuario = "Administrador",
                principal = false,
                redirecionamento =
                    "/administrador/html/Painel_administrador.html"
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
        if (!AdminAutenticado())
        {
            return Unauthorized(new
            {
                mensagem = "Acesso restrito aos administradores."
            });
        }

        return Ok(new
        {
            autenticado = true,
            tipoUsuario = "Administrador",
            email = HttpContext.Session.GetString("EmailAdmin"),
            principal = AdminPrincipalAutenticado()
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
