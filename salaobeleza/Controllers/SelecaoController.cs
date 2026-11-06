
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SelecaoController : ControllerBase
{
    private const string ChaveSelecao = "SELECAO_SERVICOS";
    private const string ChaveProfissional = "PROFISSIONAL_SELECIONADO";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [HttpGet]
    public IActionResult Get()
    {
        var selecao = ObterSelecao();

        return Ok(selecao);
    }

    [HttpPost]
    public IActionResult Post([FromBody] ItemSelecao item)
    {
        if (item == null || item.Id <= 0)
        {
            return BadRequest(new
            {
                mensagem = "O item selecionado é inválido."
            });
        }

        if (string.IsNullOrWhiteSpace(item.Tipo))
        {
            return BadRequest(new
            {
                mensagem = "O tipo do item não foi informado."
            });
        }

        var tipo = item.Tipo.Trim().ToLowerInvariant();

        if (tipo != "servico" && tipo != "combo")
        {
            return BadRequest(new
            {
                mensagem = "O tipo deve ser Servico ou Combo."
            });
        }

        if (item.Preco < 0)
        {
            return BadRequest(new
            {
                mensagem = "O preço informado é inválido."
            });
        }

        if (item.DuracaoMinutos < 0)
        {
            return BadRequest(new
            {
                mensagem = "A duração informada é inválida."
            });
        }

        var selecao = ObterSelecao();

        // Impede duplicação do mesmo serviço ou combo.
        var jaExiste = selecao.Any(x =>
            x.Id == item.Id &&
            string.Equals(
                x.Tipo,
                tipo,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (jaExiste)
        {
            return Conflict(new
            {
                mensagem = "Este item já está na sua seleção."
            });
        }

        var novoItem = new ItemSelecao
        {
            Id = item.Id,
            Nome = item.Nome?.Trim() ?? "",
            Preco = item.Preco,
            DuracaoMinutos = item.DuracaoMinutos,
            Tipo = tipo
        };

        selecao.Add(novoItem);

        SalvarSelecao(selecao);

        return Ok(selecao);
    }

    [HttpDelete]
    public IActionResult Delete()
    {
        HttpContext.Session.Remove(ChaveSelecao);
        HttpContext.Session.Remove(ChaveProfissional);

        return Ok(new
        {
            mensagem = "Seleção excluída com sucesso."
        });
    }

    [HttpDelete("{tipo}/{id:int}")]
    public IActionResult RemoverItem(string tipo, int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensagem = "Identificador inválido."
            });
        }

        var selecao = ObterSelecao();

        selecao.RemoveAll(x =>
            x.Id == id &&
            string.Equals(
                x.Tipo,
                tipo,
                StringComparison.OrdinalIgnoreCase
            )
        );

        SalvarSelecao(selecao);

        return Ok(selecao);
    }

    [HttpGet("profissional")]
    public IActionResult GetProfissional()
    {
        var json = HttpContext.Session.GetString(ChaveProfissional);

        if (string.IsNullOrWhiteSpace(json))
            return Ok(null);

        try
        {
            var profissional =
                JsonSerializer.Deserialize<ProfissionalSelecionado>(
                    json,
                    JsonOptions
                );

            return Ok(profissional);
        }
        catch (JsonException)
        {
            HttpContext.Session.Remove(ChaveProfissional);
            return Ok(null);
        }
    }

    [HttpPost("profissional")]
    public IActionResult PostProfissional(
        [FromBody] ProfissionalSelecionado profissional)
    {
        if (profissional == null || profissional.Id <= 0)
        {
            return BadRequest(new
            {
                mensagem = "A profissional selecionada é inválida."
            });
        }

        if (string.IsNullOrWhiteSpace(profissional.Nome))
        {
            return BadRequest(new
            {
                mensagem = "O nome da profissional não foi informado."
            });
        }

        HttpContext.Session.SetString(
            ChaveProfissional,
            JsonSerializer.Serialize(profissional, JsonOptions)
        );

        return Ok(profissional);
    }

    [HttpDelete("profissional")]
    public IActionResult DeleteProfissional()
    {
        HttpContext.Session.Remove(ChaveProfissional);

        return Ok(new
        {
            mensagem = "Profissional removida da seleção."
        });
    }
    private List<ItemSelecao> ObterSelecao()
    {
        var json = HttpContext.Session.GetString(ChaveSelecao);

        if (string.IsNullOrWhiteSpace(json))
            return new List<ItemSelecao>();

        try
        {
            return JsonSerializer.Deserialize<List<ItemSelecao>>(
                json,
                JsonOptions
            ) ?? new List<ItemSelecao>();
        }
        catch (JsonException)
        {
            HttpContext.Session.Remove(ChaveSelecao);

            return new List<ItemSelecao>();
        }
    }

    private void SalvarSelecao(List<ItemSelecao> selecao)
    {
        HttpContext.Session.SetString(
            ChaveSelecao,
            JsonSerializer.Serialize(selecao, JsonOptions)
        );
    }
}

public class ItemSelecao
{
    public int Id { get; set; }

    public string Nome { get; set; } = "";

    public decimal Preco { get; set; }

    public int DuracaoMinutos { get; set; }

    public string Tipo { get; set; } = "";
}

public class ProfissionalSelecionado
{
    public int Id { get; set; }

    public string Nome { get; set; } = "";
}
