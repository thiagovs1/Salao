
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;
using System.ComponentModel.DataAnnotations;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/avaliacoes")]
public class AvaliacoesController : ControllerBase
{
    private readonly AppDbContext _context;

    public AvaliacoesController(AppDbContext context)
    {
        _context = context;
    }

    private int? ClienteId =>
        HttpContext.Session.GetInt32("ClienteId");

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var clienteId = ClienteId;

        var avaliacoes = await _context.Avaliacoes
            .AsNoTracking()
            .Include(a => a.Cliente)
            .Include(a => a.Curtidas)
            .Include(a => a.Respostas)
                .ThenInclude(r => r.Cliente)
            .OrderByDescending(a => a.DataCriacao)
            .Select(a => new
            {
                id = a.Id,
                nomeCliente = a.Cliente.Nome,
                clienteId = a.ClienteId,
                nota = a.Nota,
                comentario = a.Comentario,
                dataCriacao = a.DataCriacao,
                curtidas = a.Curtidas.Count(),
                curtidoPorMim = clienteId.HasValue &&
                    a.Curtidas.Any(c => c.ClienteId == clienteId.Value),
                respostas = a.Respostas
                    .OrderBy(r => r.DataCriacao)
                    .Select(r => new
                    {
                        id = r.Id,
                        nomeCliente = r.Cliente.Nome,
                        clienteId = r.ClienteId,
                        resposta = r.Resposta,
                        dataCriacao = r.DataCriacao
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(avaliacoes);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarAvaliacaoDTO dto)
    {
        var clienteId = ClienteId;

        if (clienteId == null)
        {
            return Unauthorized(new
            {
                mensagem = "Entre na sua conta para publicar uma avaliação."
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                mensagem = "Informe uma nota de 1 a 5 estrelas e um comentário de até 1000 caracteres."
            });
        }

        var clienteExiste = await _context.Clientes
            .AnyAsync(c => c.Id == clienteId.Value);

        if (!clienteExiste)
        {
            return Unauthorized(new
            {
                mensagem = "Não foi possível identificar sua conta. Entre novamente."
            });
        }

        var avaliacao = new Avaliacao
        {
            ClienteId = clienteId.Value,
            Nota = dto.Nota,
            Comentario = dto.Comentario.Trim(),
            DataCriacao = DateTime.Now
        };

        _context.Avaliacoes.Add(avaliacao);
        await _context.SaveChangesAsync();

        var cliente = await _context.Clientes
            .Where(c => c.Id == clienteId.Value)
            .Select(c => c.Nome)
            .FirstAsync();

        return Created($"/api/avaliacoes/{avaliacao.Id}", new
        {
            id = avaliacao.Id,
            nomeCliente = cliente,
            clienteId = clienteId.Value,
            nota = avaliacao.Nota,
            comentario = avaliacao.Comentario,
            dataCriacao = avaliacao.DataCriacao,
            curtidas = 0,
            curtidoPorMim = false,
            respostas = Array.Empty<object>()
        });
    }

    [HttpPost("{id:int}/curtidas")]
    public async Task<IActionResult> Curtir(int id)
    {
        var clienteId = ClienteId;

        if (clienteId == null)
        {
            return Unauthorized(new
            {
                mensagem = "Entre na sua conta para curtir uma avaliação."
            });
        }

        var existe = await _context.Avaliacoes
            .AnyAsync(a => a.Id == id);

        if (!existe)
        {
            return NotFound(new
            {
                mensagem = "Esta avaliação não foi encontrada."
            });
        }

        var jaCurtiu = await _context.AvaliacaoCurtidas
            .AnyAsync(c => c.AvaliacaoId == id &&
                           c.ClienteId == clienteId.Value);

        if (!jaCurtiu)
        {
            _context.AvaliacaoCurtidas.Add(new AvaliacaoCurtida
            {
                AvaliacaoId = id,
                ClienteId = clienteId.Value,
                DataCriacao = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        var totalCurtidas = await _context.AvaliacaoCurtidas
            .CountAsync(c => c.AvaliacaoId == id);

        return Ok(new
        {
            curtidas = totalCurtidas,
            curtidoPorMim = true
        });
    }

    [HttpPost("{id:int}/respostas")]
    public async Task<IActionResult> Responder(
        int id,
        [FromBody] CriarRespostaDTO dto)
    {
        var clienteId = ClienteId;

        if (clienteId == null)
        {
            return Unauthorized(new
            {
                mensagem = "Entre na sua conta para responder."
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                mensagem = "Escreva uma resposta com até 1000 caracteres."
            });
        }

        var avaliacaoExiste = await _context.Avaliacoes
            .AnyAsync(a => a.Id == id);

        if (!avaliacaoExiste)
        {
            return NotFound(new
            {
                mensagem = "Esta avaliação não foi encontrada."
            });
        }

        var resposta = new AvaliacaoResposta
        {
            AvaliacaoId = id,
            ClienteId = clienteId.Value,
            Resposta = dto.Resposta.Trim(),
            DataCriacao = DateTime.Now
        };

        _context.AvaliacaoRespostas.Add(resposta);
        await _context.SaveChangesAsync();

        var nomeCliente = await _context.Clientes
            .Where(c => c.Id == clienteId.Value)
            .Select(c => c.Nome)
            .FirstAsync();

        return Created(
            $"/api/avaliacoes/{id}/respostas/{resposta.Id}",
            new
            {
                id = resposta.Id,
                nomeCliente,
                clienteId = clienteId.Value,
                resposta = resposta.Resposta,
                dataCriacao = resposta.DataCriacao
            });
    }
}

public class CriarAvaliacaoDTO
{
    [Range(1, 5)]
    public int Nota { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 2)]
    public string Comentario { get; set; } = string.Empty;
}

public class CriarRespostaDTO
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Resposta { get; set; } = string.Empty;
}
