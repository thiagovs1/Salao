
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CombosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CombosController> _logger;

    public CombosController(
        AppDbContext context,
        Cloudinary cloudinary,
        ILogger<CombosController> logger)
    {
        _context = context;
        _cloudinary = cloudinary;
        _logger = logger;
    }

    public class ComboSalvarDTO
    {
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public List<int> ServicoIds { get; set; } = new();
    }

    [HttpGet]
    public async Task<IActionResult> GetCombos()
    {
        var combos = await _context.Combos
            .Include(c => c.ComboServicos)
                .ThenInclude(cs => cs.Servico)
            .Select(c => new
            {
                id = c.Id,
                nome = c.Nome,
                preco = c.Preco,
                imagem = c.Imagem,
                servicos = c.ComboServicos.Select(cs => new
                {
                    id = cs.Servico.Id,
                    nome = cs.Servico.Nome,
                    preco = cs.Servico.Preco,
                    duracaoMinutos = cs.Servico.DuracaoMinutos
                }).ToList()
            })
            .ToListAsync();

        return Ok(combos);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] ComboSalvarDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            return BadRequest(new { mensagem = "Informe o nome do combo." });

        if (dto.Nome.Trim().Length > 100)
            return BadRequest(new { mensagem = "O nome pode ter no máximo 100 caracteres." });

        if (dto.Preco <= 0)
            return BadRequest(new { mensagem = "O preço deve ser maior que zero." });

        if (dto.ServicoIds == null || dto.ServicoIds.Count == 0)
            return BadRequest(new { mensagem = "Selecione pelo menos um serviço." });

        var ids = dto.ServicoIds.Distinct().ToList();

        var servicos = await _context.Servicos
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        if (servicos.Count != ids.Count)
            return BadRequest(new { mensagem = "Um ou mais serviços não existem." });

        var combo = new Combo
        {
            Nome = dto.Nome.Trim(),
            Preco = dto.Preco
        };

        foreach (var servico in servicos)
        {
            combo.ComboServicos.Add(new ComboServico
            {
                Servico = servico
            });
        }

        _context.Combos.Add(combo);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Combo cadastrado com sucesso.",
            id = combo.Id,
            imagem = combo.Imagem
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] ComboSalvarDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            return BadRequest(new { mensagem = "Informe o nome do combo." });

        if (dto.Nome.Trim().Length > 100)
            return BadRequest(new { mensagem = "O nome pode ter no máximo 100 caracteres." });

        if (dto.Preco <= 0)
            return BadRequest(new { mensagem = "O preço deve ser maior que zero." });

        if (dto.ServicoIds == null || dto.ServicoIds.Count == 0)
            return BadRequest(new { mensagem = "Selecione pelo menos um serviço." });

        var combo = await _context.Combos
            .Include(c => c.ComboServicos)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (combo == null)
            return NotFound(new { mensagem = "Combo não encontrado." });

        var ids = dto.ServicoIds.Distinct().ToList();

        var servicos = await _context.Servicos
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        if (servicos.Count != ids.Count)
            return BadRequest(new { mensagem = "Um ou mais serviços não existem." });

        combo.Nome = dto.Nome.Trim();
        combo.Preco = dto.Preco;

        _context.Set<ComboServico>().RemoveRange(combo.ComboServicos);
        combo.ComboServicos.Clear();

        foreach (var servico in servicos)
        {
            combo.ComboServicos.Add(new ComboServico
            {
                ComboId = combo.Id,
                ServicoId = servico.Id
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Combo atualizado com sucesso.",
            imagem = combo.Imagem
        });
    }

    // ENVIO DA IMAGEM PARA O CLOUDINARY

    [HttpPost("{id:int}/imagem")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> EnviarImagem(
        int id,
        [FromForm] IFormFile arquivo)
    {
        var combo = await _context.Combos
            .FirstOrDefaultAsync(c => c.Id == id);

        if (combo == null)
            return NotFound(new { mensagem = "Combo não encontrado." });

        if (arquivo == null || arquivo.Length == 0)
            return BadRequest(new { mensagem = "Selecione uma imagem." });

        if (arquivo.Length > 5 * 1024 * 1024)
            return BadRequest(new { mensagem = "A imagem deve ter no máximo 5 MB." });

        var extensao = Path.GetExtension(arquivo.FileName)
            .ToLowerInvariant();

        var tiposPermitidos = new Dictionary<string, string>
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

        if (!tiposPermitidos.TryGetValue(extensao, out var tipoEsperado) ||
            !string.Equals(
                arquivo.ContentType,
                tipoEsperado,
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                mensagem = "Use uma imagem JPG, PNG ou WEBP válida."
            });
        }

        try
        {
            await using var stream = arquivo.OpenReadStream();

            // Cada combo possui um endereço próprio no Cloudinary.
            // Ao trocar a imagem, o arquivo anterior é substituído.

            var publicId = $"belleza/combos/combo_{combo.Id}";

            var parametros = new ImageUploadParams
            {
                File = new FileDescription(
                    arquivo.FileName,
                    stream
                ),

                PublicId = publicId,
                Overwrite = true,
                Invalidate = true
            };

            var resultado = await _cloudinary.UploadAsync(parametros);

            if (resultado.Error != null)
            {
                _logger.LogError(
                    "Erro no Cloudinary: {Erro}",
                    resultado.Error.Message
                );

                return StatusCode(502, new
                {
                    mensagem = "Não foi possível enviar a imagem para o Cloudinary.",
                    erro = resultado.Error.Message
                });
            }

            if (resultado.SecureUrl == null)
            {
                return StatusCode(502, new
                {
                    mensagem = "O Cloudinary não retornou o endereço da imagem."
                });
            }

            // Salva o endereço permanente no MySQL.

            combo.Imagem = resultado.SecureUrl.ToString();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Imagem do combo atualizada com sucesso.",
                imagem = combo.Imagem
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao atualizar imagem do combo {ComboId}",
                id
            );

            return StatusCode(500, new
            {
                mensagem = "Ocorreu um erro ao atualizar a imagem do combo."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var combo = await _context.Combos
            .Include(c => c.ComboServicos)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (combo == null)
            return NotFound(new { mensagem = "Combo não encontrado." });

        try
        {
            _context.Set<ComboServico>()
                .RemoveRange(combo.ComboServicos);

            _context.Combos.Remove(combo);

            await _context.SaveChangesAsync();

            // Remove também a imagem armazenada na nuvem.

            try
            {
                await _cloudinary.DestroyAsync(
                    new DeletionParams(
                        $"belleza/combos/combo_{id}"
                    )
                    {
                        Invalidate = true
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Não foi possível excluir a imagem do combo {ComboId} no Cloudinary.",
                    id
                );
            }

            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                mensagem = "Não foi possível excluir este combo. Ele pode estar vinculado a agendamentos."
            });
        }
    }
}
