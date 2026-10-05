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
                email = usuario.Email
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
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

        // Busca o cliente correspondente ao usuário logado.
        // Usuario e Cliente são tabelas diferentes,
        // então não podemos usar o ID do Usuario como ClienteId.
        var cliente = await _service.BuscarClientePorEmail(usuario.Email);

        if (cliente == null)
        {
            return Unauthorized(new
            {
                mensagem = "Cliente não encontrado para esta conta."
            });
        }

        // Guarda o ID real do Cliente na sessão.
        HttpContext.Session.SetInt32(
            "ClienteId",
            cliente.Id
        );

        return Ok(new
        {
            id = usuario.Id,
            nome = usuario.Nome,
            email = usuario.Email
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
}