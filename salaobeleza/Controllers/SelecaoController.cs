
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
        var json = HttpContext.Session.GetString(ChaveSelecao);

        if (string.IsNullOrWhiteSpace(json))
            return Ok(new List<ItemSelecao>());

        try
        {
            return Ok(
                JsonSerializer.Deserialize<List<ItemSelecao>>(json, JsonOptions)
                ?? new List<ItemSelecao>()
            );
        }
        catch (JsonException)
        {
            return Ok(new List<ItemSelecao>());
        }
    }

    [HttpPost]
    public IActionResult Post([FromBody] ItemSelecao item)
    {
        if (item == null || item.Id <= 0)
            return BadRequest(new { mensagem = "O item selecionado é inválido." });

        if (string.IsNullOrWhiteSpace(item.Tipo))
            return BadRequest(new { mensagem = "O tipo do item não foi informado." });

        var tipo = item.Tipo.Trim();

        if (!tipo.Equals("Servico", StringComparison.OrdinalIgnoreCase) &&
            !tipo.Equals("Combo", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "O tipo deve ser Servico ou Combo." });
        }

        var json = HttpContext.Session.GetString(ChaveSelecao);

        var selecao = string.IsNullOrWhiteSpace(json)
            ? new List<ItemSelecao>()
            : JsonSerializer.Deserialize<List<ItemSelecao>>(json, JsonOptions)
                ?? new List<ItemSelecao>();

        // Não adiciona novamente o mesmo serviço ou combo.
        if (selecao.Any(x =>
            x.Id == item.Id &&
            x.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase)))
        {
            return Ok(selecao);
        }

        selecao.Add(new ItemSelecao
        {
            Id = item.Id,
            Nome = item.Nome?.Trim() ?? "",
            Preco = item.Preco,
            Tipo = tipo
        });

        HttpContext.Session.SetString(
            ChaveSelecao,
            JsonSerializer.Serialize(selecao, JsonOptions)
        );

        return Ok(selecao);
    }

    [HttpDelete]
    public IActionResult Delete()
    {
        HttpContext.Session.Remove(ChaveSelecao);
        HttpContext.Session.Remove(ChaveProfissional);

        return Ok(new { mensagem = "Seleção excluída com sucesso." });
    }

    [HttpDelete("{tipo}/{id:int}")]
    public IActionResult RemoverItem(string tipo, int id)
    {
        var json = HttpContext.Session.GetString(ChaveSelecao);

        var selecao = string.IsNullOrWhiteSpace(json)
            ? new List<ItemSelecao>()
            : JsonSerializer.Deserialize<List<ItemSelecao>>(json, JsonOptions)
                ?? new List<ItemSelecao>();

        selecao.RemoveAll(x =>
            x.Id == id &&
            x.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase));

        HttpContext.Session.SetString(
            ChaveSelecao,
            JsonSerializer.Serialize(selecao, JsonOptions)
        );

        return Ok(selecao);
    }

    [HttpGet("profissional")]
    public IActionResult GetProfissional()
    {
        var json = HttpContext.Session.GetString(ChaveProfissional);

        if (string.IsNullOrWhiteSpace(json))
            return Ok(null);

        return Ok(
            JsonSerializer.Deserialize<ProfissionalSelecionado>(json, JsonOptions)
        );
    }

    [HttpPost("profissional")]
    public IActionResult PostProfissional(
        [FromBody] ProfissionalSelecionado profissional)
    {
        if (profissional == null || profissional.Id <= 0)
            return BadRequest(new { mensagem = "A profissional selecionada é inválida." });

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

        return Ok(new { mensagem = "Profissional removida da seleção." });
    }
}

public class ItemSelecao
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public decimal Preco { get; set; }
    public string Tipo { get; set; } = "";
}

public class ProfissionalSelecionado
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
}
