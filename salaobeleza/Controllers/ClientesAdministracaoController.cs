using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesAdministracaoController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClientesAdministracaoController(AppDbContext db)
    {
        _db = db;
    }

    private bool AdministradorAutenticado()
    {
        return HttpContext.Session.GetString("AdministradorAutenticado")
                   ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
               && HttpContext.Session.GetString("TipoUsuarioAdmin") == "Administrador"
               && !string.IsNullOrWhiteSpace(
                   HttpContext.Session.GetString("EmailAdmin"));
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        if (!AdministradorAutenticado())
            return Unauthorized(new { mensagem = "Acesso restrito ao administrador." });

        var clientes = await _db.Clientes
            .AsNoTracking()
            .Include(c => c.Agendamentos)
                .ThenInclude(a => a.Servico)
            .Include(c => c.Agendamentos)
                .ThenInclude(a => a.Combo)
                    .ThenInclude(c => c!.ComboServicos)
                        .ThenInclude(cs => cs.Servico)
            .Include(c => c.Agendamentos)
                .ThenInclude(a => a.AgendamentoServicos)
                    .ThenInclude(a => a.Servico)
            .OrderBy(c => c.Nome)
            .ToListAsync();

        var resultado = clientes.Select(cliente =>
        {
            decimal totalGasto = 0;
            int quantidadeServicos = 0;

            foreach (var agendamento in cliente.Agendamentos)
            {
                var status = agendamento.Status?.Trim().ToLowerInvariant() ?? "";

                if (status.Contains("cancel"))
                    continue;

                if (agendamento.Combo != null)
                {
                    totalGasto += agendamento.Combo.Preco;
                    quantidadeServicos +=
                        agendamento.Combo.ComboServicos?.Count ?? 0;
                }
                else if (agendamento.AgendamentoServicos.Count > 0)
                {
                    totalGasto += agendamento.AgendamentoServicos
                        .Where(x => x.Servico != null)
                        .Sum(x => x.Servico!.Preco);

                    quantidadeServicos += agendamento.AgendamentoServicos.Count;
                }
                else if (agendamento.Servico != null)
                {
                    totalGasto += agendamento.Servico.Preco;
                    quantidadeServicos++;
                }
            }

            return new
            {
                cliente.Id,
                cliente.Nome,
                cliente.Email,
                cliente.Telefone,
                cliente.DataCadastro,
                TotalGasto = totalGasto,
                QuantidadeServicos = quantidadeServicos
            };
        }).ToList();

        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] ClienteAtualizacaoRequest dados)
    {
        if (!AdministradorAutenticado())
            return Unauthorized(new { mensagem = "Acesso restrito ao administrador." });

        if (string.IsNullOrWhiteSpace(dados.Nome) ||
            string.IsNullOrWhiteSpace(dados.Email) ||
            string.IsNullOrWhiteSpace(dados.Telefone))
        {
            return BadRequest(new
            {
                mensagem = "Preencha nome, e-mail e telefone."
            });
        }

        var cliente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cliente == null)
            return NotFound(new { mensagem = "Cliente não encontrado." });

        var email = dados.Email.Trim();
        var nome = dados.Nome.Trim();
        var telefone = dados.Telefone.Trim();

        if (!email.Contains('@'))
            return BadRequest(new { mensagem = "Informe um e-mail válido." });

        var emailEmUso = await _db.Clientes.AnyAsync(c =>
            c.Id != id && c.Email == email);

        if (emailEmUso)
            return Conflict(new { mensagem = "Esse e-mail já está cadastrado." });

        var telefoneEmUso = await _db.Clientes.AnyAsync(c =>
            c.Id != id && c.Telefone == telefone);

        if (telefoneEmUso)
            return Conflict(new { mensagem = "Esse telefone já está cadastrado." });

        var emailAntigo = cliente.Email;

        cliente.Nome = nome;
        cliente.Email = email;
        cliente.Telefone = telefone;

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u =>
            u.Email == emailAntigo && u.TipoUsuario == "Cliente");

        if (usuario != null)
        {
            var emailUsuarioEmUso = await _db.Usuarios.AnyAsync(u =>
                u.Id != usuario.Id && u.Email == email);

            if (emailUsuarioEmUso)
                return Conflict(new
                {
                    mensagem = "Esse e-mail já está vinculado a outra conta."
                });

            usuario.Nome = nome;
            usuario.Email = email;
        }

        await _db.SaveChangesAsync();

        return Ok(new { mensagem = "Cliente atualizado com sucesso." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        if (!AdministradorAutenticado())
            return Unauthorized(new { mensagem = "Acesso restrito ao administrador." });

        var cliente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cliente == null)
            return NotFound(new { mensagem = "Cliente não encontrado." });

        var agendamentos = await _db.Agendamentos
            .Where(a => a.ClienteId == id)
            .ToListAsync();

        var avaliacoes = await _db.Avaliacoes
            .Where(a => a.ClienteId == id)
            .ToListAsync();

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u =>
            u.Email == cliente.Email && u.TipoUsuario == "Cliente");

        _db.Agendamentos.RemoveRange(agendamentos);
        _db.Avaliacoes.RemoveRange(avaliacoes);

        if (usuario != null)
            _db.Usuarios.Remove(usuario);

        _db.Clientes.Remove(cliente);

        await _db.SaveChangesAsync();

        return Ok(new { mensagem = "Cliente excluído com sucesso." });
    }
}

public record ClienteAtualizacaoRequest(
    string Nome,
    string Email,
    string Telefone
);