using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SelecaoController : ControllerBase
{
    private const string ChaveSelecao = "SELECAO_SERVICOS";
    private const string ChaveProfissional = "PROFISSIONAL_SELECIONADO";

    [HttpGet]
    public IActionResult Get()
    {
        var json = HttpContext.Session.GetString(ChaveSelecao);

        if (string.IsNullOrEmpty(json))
            return Ok(new List<ItemSelecao>());

        var selecao = JsonSerializer.Deserialize<List<ItemSelecao>>(json);

        return Ok(selecao ?? new List<ItemSelecao>());
    }

    [HttpPost]
    public IActionResult Post([FromBody] ItemSelecao item)
    {
        Console.WriteLine($"[SELECAO] ID recebido: {item.Id}");
        Console.WriteLine($"[SELECAO] Nome recebido: {item.Nome}");
        Console.WriteLine($"[SELECAO] Preço recebido: {item.Preco}");
        Console.WriteLine($"[SELECAO] Tipo recebido: {item.Tipo}");

        if (item.Id <= 0)
        {
            return BadRequest(new
            {
                mensagem = "O ID do serviço/combo recebido é inválido.",
                idRecebido = item.Id,
                nomeRecebido = item.Nome,
                precoRecebido = item.Preco,
                tipoRecebido = item.Tipo
            });
        }

        var json = HttpContext.Session.GetString(ChaveSelecao);

        var selecao = string.IsNullOrEmpty(json)
            ? new List<ItemSelecao>()
            : JsonSerializer.Deserialize<List<ItemSelecao>>(json)
                ?? new List<ItemSelecao>();

        selecao.Add(item);

        HttpContext.Session.SetString(
            ChaveSelecao,
            JsonSerializer.Serialize(selecao)
        );

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

    [HttpGet("profissional")]
    public IActionResult GetProfissional()
    {
        var json = HttpContext.Session.GetString(ChaveProfissional);

        if (string.IsNullOrEmpty(json))
            return Ok(null);

        var profissional =
            JsonSerializer.Deserialize<ProfissionalSelecionado>(json);

        return Ok(profissional);
    }

    [HttpPost("profissional")]
    public IActionResult PostProfissional(
        [FromBody] ProfissionalSelecionado profissional)
    {
        HttpContext.Session.SetString(
            ChaveProfissional,
            JsonSerializer.Serialize(profissional)
        );

        return Ok(profissional);
    }

    [HttpDelete("profissional")]
    public IActionResult DeleteProfissional()
    {
        HttpContext.Session.Remove(ChaveProfissional);

        return Ok(new
        {
            mensagem = "Profissional removido da seleção."
        });
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