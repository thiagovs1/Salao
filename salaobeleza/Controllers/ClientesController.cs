
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.DTOs;
using SalaoBeleza.Models;
using SalaoBeleza.Services;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _service;
    private readonly AppDbContext _db;

    public ClientesController(ClienteService service, AppDbContext db)
    {
        _service = service;
        _db = db;
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

        var cliente = await _db.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clienteId.Value);

        if (cliente == null)
            return NotFound(new { mensagem = "Cliente não encontrado." });

        return Ok(new
        {
            id = cliente.Id,
            nome = cliente.Nome,
            email = cliente.Email,
            telefone = cliente.Telefone,
            cpf = cliente.CPF ?? "",
            endereco = cliente.Endereco ?? ""
        });
    }

    [HttpPut("meu-perfil")]
    public async Task<IActionResult> AtualizarMeuPerfil(
        [FromBody] ClientePerfilAtualizarDTO dto)
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");

        if (clienteId == null)
            return Unauthorized(new { mensagem = "Faça login para continuar." });

        var cliente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.Id == clienteId.Value);

        if (cliente == null)
            return NotFound(new { mensagem = "Cliente não encontrado." });

        var nome = dto.Nome.Trim();
        var email = dto.Email.Trim();
        var telefone = dto.Telefone.Trim();
        var cpf = string.IsNullOrWhiteSpace(dto.CPF)
            ? null
            : dto.CPF.Trim();
        var endereco = string.IsNullOrWhiteSpace(dto.Endereco)
            ? null
            : dto.Endereco.Trim();

        if (string.IsNullOrWhiteSpace(nome) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(telefone))
        {
            return BadRequest(new
            {
                mensagem = "Preencha nome, e-mail e telefone."
            });
        }

        var emailEmUso = await _db.Clientes.AnyAsync(c =>
            c.Id != cliente.Id && c.Email == email);

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u =>
                u.Email == cliente.Email &&
                u.TipoUsuario == "Cliente");

        var emailDeUsuarioEmUso = await _db.Usuarios.AnyAsync(u =>
            u.Email == email &&
            (usuario == null || u.Id != usuario.Id));

        if (emailEmUso || emailDeUsuarioEmUso)
        {
            return Conflict(new
            {
                mensagem = "Este e-mail já está sendo usado por outra conta."
            });
        }

        if (usuario == null)
        {
            return Conflict(new
            {
                mensagem = "Não foi encontrada a conta de acesso vinculada a este cliente."
            });
        }

        cliente.Nome = nome;
        cliente.Email = email;
        cliente.Telefone = telefone;
        cliente.CPF = cpf;
        cliente.Endereco = endereco;

        // Mantém os dados de acesso sincronizados com o perfil.
        usuario.Nome = nome;
        usuario.Email = email;

        try
        {
            await _db.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Perfil atualizado com sucesso.",
                nome = cliente.Nome,
                email = cliente.Email,
                telefone = cliente.Telefone,
                cpf = cliente.CPF ?? "",
                endereco = cliente.Endereco ?? ""
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                mensagem = "Não foi possível salvar os dados. Verifique se o e-mail ou telefone já está cadastrado."
            });
        }
    }

    [HttpDelete("meu-perfil")]
    public async Task<IActionResult> ApagarMeuPerfil()
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");

        if (clienteId == null)
            return Unauthorized(new { mensagem = "Faça login para continuar." });

        var cliente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.Id == clienteId.Value);

        if (cliente == null)
        {
            HttpContext.Session.Clear();
            return NotFound(new { mensagem = "Cliente não encontrado." });
        }

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u =>
                u.Email == cliente.Email &&
                u.TipoUsuario == "Cliente");

        // Remove os registros relacionados antes de apagar o cliente.
        var agendamentos = await _db.Agendamentos
            .Where(a => a.ClienteId == cliente.Id)
            .ToListAsync();

        var avaliacoes = await _db.Avaliacoes
            .Where(a => a.ClienteId == cliente.Id)
            .ToListAsync();

        _db.Agendamentos.RemoveRange(agendamentos);
        _db.Avaliacoes.RemoveRange(avaliacoes);

        if (usuario != null)
            _db.Usuarios.Remove(usuario);

        _db.Clientes.Remove(cliente);

        try
        {
            await _db.SaveChangesAsync();
            HttpContext.Session.Clear();

            return Ok(new
            {
                mensagem = "Conta excluída com sucesso."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                mensagem = "Não foi possível excluir a conta por causa de registros vinculados."
            });
        }
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
