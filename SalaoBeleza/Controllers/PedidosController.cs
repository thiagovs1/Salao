using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly AppDbContext _context;

    public PedidosController(AppDbContext context)
    {
        _context = context;
    }

    private int? ObterClienteId()
    {
        if (!Request.Cookies.TryGetValue("clienteId", out string? valor))
            return null;

        if (!int.TryParse(valor, out int clienteId))
            return null;

        return clienteId;
    }


    // =====================================================
    // SELECIONAR COMBO
    // =====================================================

    [HttpPost("combo")]
    public async Task<IActionResult> SelecionarCombo(
        [FromBody] SelecionarComboRequest request)
    {
        try
        {
            var clienteId = ObterClienteId();

            if (clienteId == null)
            {
                return Unauthorized(new
                {
                    mensagem = "Faça login para continuar."
                });
            }


            var combo = await _context.Combos
                .FirstOrDefaultAsync(c => c.Id == request.IdCombo);


            if (combo == null)
            {
                return NotFound(new
                {
                    mensagem = "Combo não encontrado."
                });
            }


            var servicosCombo = await _context.ComboServicos
                .Where(cs => cs.ComboId == request.IdCombo)
                .Select(cs => cs.ServicoId)
                .ToListAsync();


            if (servicosCombo.Count == 0)
            {
                return BadRequest(new
                {
                    mensagem = "Esse combo não possui serviços cadastrados."
                });
            }


            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p =>
                    p.ClienteId == clienteId &&
                    p.Status == "Em andamento");


            if (pedido == null)
            {
                pedido = new Pedido
                {
                    ClienteId = clienteId.Value,
                    ComboId = request.IdCombo,
                    Status = "Em andamento",
                    DataCriacao = DateTime.Now
                };

                _context.Pedidos.Add(pedido);

                await _context.SaveChangesAsync();
            }
            else
            {
                /*
                 * Mantém os serviços que o cliente
                 * já havia selecionado.
                 *
                 * Apenas troca o combo.
                 */

                pedido.ComboId = request.IdCombo;

                await _context.SaveChangesAsync();
            }


            /*
             * Adiciona os serviços do combo.
             *
             * Se o serviço já estiver no pedido,
             * não adiciona novamente.
             */

            foreach (var idServico in servicosCombo.Distinct())
            {
                var jaExiste = await _context.PedidoServicos
                    .AnyAsync(ps =>
                        ps.PedidoId == pedido.Id &&
                        ps.ServicoId == idServico);


                if (!jaExiste)
                {
                    _context.PedidoServicos.Add(
                        new PedidoServico
                        {
                            PedidoId = pedido.Id,
                            ServicoId = idServico
                        }
                    );
                }
            }


            await _context.SaveChangesAsync();


            return Ok(new
            {
                mensagem = "Combo selecionado com sucesso.",
                pedidoId = pedido.Id,
                clienteId = clienteId.Value,
                comboId = combo.Id,
                combo = combo.Nome,
                preco = combo.Preco
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensagem = "Erro ao salvar o pedido.",
                erro = ex.Message,
                detalhe = ex.InnerException?.Message
            });
        }
    }


    // =====================================================
    // SELECIONAR SERVIÇO INDIVIDUAL
    // =====================================================

    [HttpPost("servico")]
    public async Task<IActionResult> SelecionarServico(
        [FromBody] SelecionarServicoRequest request)
    {
        try
        {
            var clienteId = ObterClienteId();

            if (clienteId == null)
            {
                return Unauthorized(new
                {
                    mensagem = "Faça login para continuar."
                });
            }


            if (string.IsNullOrWhiteSpace(request.NomeServico))
            {
                return BadRequest(new
                {
                    mensagem = "O nome do serviço não foi informado."
                });
            }


            var servico = await _context.Servicos
                .FirstOrDefaultAsync(s =>
                    s.Nome == request.NomeServico);


            if (servico == null)
            {
                return NotFound(new
                {
                    mensagem = "Serviço não encontrado no banco de dados."
                });
            }


            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p =>
                    p.ClienteId == clienteId &&
                    p.Status == "Em andamento");


            if (pedido == null)
            {
                pedido = new Pedido
                {
                    ClienteId = clienteId.Value,
                    ComboId = null,
                    Status = "Em andamento",
                    DataCriacao = DateTime.Now
                };

                _context.Pedidos.Add(pedido);

                await _context.SaveChangesAsync();
            }
            else
            {
                /*
                 * Se o cliente escolher um serviço individual,
                 * o pedido passa a ser sem combo.
                 */

                pedido.ComboId = null;

                await _context.SaveChangesAsync();
            }


            var servicoJaSelecionado =
                await _context.PedidoServicos
                    .AnyAsync(ps =>
                        ps.PedidoId == pedido.Id &&
                        ps.ServicoId == servico.Id);


            if (!servicoJaSelecionado)
            {
                _context.PedidoServicos.Add(
                    new PedidoServico
                    {
                        PedidoId = pedido.Id,
                        ServicoId = servico.Id
                    }
                );

                await _context.SaveChangesAsync();
            }


            return Ok(new
            {
                mensagem = "Serviço selecionado com sucesso.",
                pedidoId = pedido.Id,
                clienteId = clienteId.Value,
                comboId = (int?)null,

                servico = new
                {
                    id = servico.Id,
                    nome = servico.Nome,
                    categoria = servico.Categoria,
                    preco = servico.Preco,
                    duracaoMinutos = servico.DuracaoMinutos
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensagem = "Erro ao selecionar o serviço.",
                erro = ex.Message,
                detalhe = ex.InnerException?.Message
            });
        }
    }


    // =====================================================
    // BUSCAR PEDIDO DO CLIENTE
    // =====================================================

    [HttpGet("meu-pedido")]
    public async Task<IActionResult> MeuPedido()
    {
        try
        {
            var clienteId = ObterClienteId();

            if (clienteId == null)
            {
                return Unauthorized(new
                {
                    mensagem = "Faça login para continuar."
                });
            }


            var pedido = await _context.Pedidos
                .Include(p => p.Combo)
                .Include(p => p.PedidoServicos)
                    .ThenInclude(ps => ps.Servico)
                .FirstOrDefaultAsync(p =>
                    p.ClienteId == clienteId &&
                    p.Status == "Em andamento");


            if (pedido == null)
            {
                return NotFound(new
                {
                    mensagem = "Você ainda não possui um pedido."
                });
            }


            var servicos = pedido.PedidoServicos
                .Select(ps => new
                {
                    id = ps.Servico.Id,
                    nome = ps.Servico.Nome,
                    categoria = ps.Servico.Categoria,
                    preco = ps.Servico.Preco,
                    duracaoMinutos = ps.Servico.DuracaoMinutos,

                    doCombo = pedido.Combo != null &&
                              _context.ComboServicos.Any(cs =>
                                  cs.ComboId == pedido.Combo.Id &&
                                  cs.ServicoId == ps.Servico.Id)
                })
                .ToList();


            return Ok(new
            {
                id = pedido.Id,

                clienteId = pedido.ClienteId,

                combo = pedido.Combo == null
                    ? null
                    : new
                    {
                        id = pedido.Combo.Id,
                        nome = pedido.Combo.Nome,
                        preco = pedido.Combo.Preco
                    },

                servicos = servicos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensagem = "Erro ao buscar o pedido.",
                erro = ex.Message,
                detalhe = ex.InnerException?.Message
            });
        }
    }
}


// =========================================================
// REQUEST COMBO
// =========================================================

public class SelecionarComboRequest
{
    public int IdCombo { get; set; }
}


// =========================================================
// REQUEST SERVIÇO
// =========================================================

public class SelecionarServicoRequest
{
    public string NomeServico { get; set; } = string.Empty;
}