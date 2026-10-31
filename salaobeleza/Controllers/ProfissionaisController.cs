
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Models;
using System.Globalization;
using System.Text;

namespace SalaoBeleza.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfissionaisController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<ProfissionaisController> _logger;

    public ProfissionaisController(
        AppDbContext context,
        Cloudinary cloudinary,
        ILogger<ProfissionaisController> logger)
    {
        _context = context;
        _cloudinary = cloudinary;
        _logger = logger;
    }

    public class ProfissionalFormulario
    {
        public string Nome { get; set; } = "";
        public string Profissao { get; set; } = "";
        public string? CPF { get; set; }
        public string? Telefone { get; set; }
        public string? Endereco { get; set; }
        public decimal TaxaAtendimento { get; set; }
        public string? Senha { get; set; }
        public IFormFile? Foto { get; set; }
        public List<int> ServicoIds { get; set; } = new();
        public List<HorarioFormulario> Horarios { get; set; } = new();
    }

    public class HorarioFormulario
    {
        public int DiaSemana { get; set; }
        public string HoraInicio { get; set; } = "";
        public string HoraFim { get; set; } = "";
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var profissionais = await _context.Profissionais
            .Where(p => p.Ativo)
            .Select(p => new
            {
                id = p.Id,
                nome = p.Nome,
                profissao = p.Profissao,
                cpf = p.CPF,
                telefone = p.Telefone,
                endereco = p.Endereco,
                taxaAtendimento = p.TaxaAtendimento,
                foto = p.Foto,
                imagem = p.Foto,
                email = _context.Usuarios
                    .Where(u => u.ProfissionalId == p.Id)
                    .Select(u => u.Email)
                    .FirstOrDefault(),
                servicos = p.ServicosProfissionais
                    .Select(sp => new
                    {
                        id = sp.ServicoId,
                        nome = sp.Servico.Nome
                    }).ToList(),
                horarios = p.HorariosProfissionais
                    .OrderBy(h => h.DiaSemana)
                    .ThenBy(h => h.HoraInicio)
                    .Select(h => new
                    {
                        diaSemana = h.DiaSemana,
                        horaInicio = h.HoraInicio.ToString(),
                        horaFim = h.HoraFim.ToString()
                    }).ToList()
            })
            .ToListAsync();

        return Ok(profissionais);
    }

    [HttpGet("servicos-disponiveis")]
    public async Task<IActionResult> ServicosDisponiveis()
    {
        var servicos = await _context.Servicos
            .OrderBy(s => s.Nome)
            .Select(s => new
            {
                id = s.Id,
                nome = s.Nome,
                preco = s.Preco,
                duracaoMinutos = s.DuracaoMinutos
            })
            .ToListAsync();

        return Ok(servicos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPorId(int id)
    {
        var profissional = await _context.Profissionais
            .Where(p => p.Id == id)
            .Select(p => new
            {
                id = p.Id,
                nome = p.Nome,
                profissao = p.Profissao,
                cpf = p.CPF,
                telefone = p.Telefone,
                endereco = p.Endereco,
                taxaAtendimento = p.TaxaAtendimento,
                foto = p.Foto,
                imagem = p.Foto,
                email = _context.Usuarios
                    .Where(u => u.ProfissionalId == p.Id)
                    .Select(u => u.Email)
                    .FirstOrDefault(),
                servicoIds = p.ServicosProfissionais
                    .Select(sp => sp.ServicoId).ToList(),
                horarios = p.HorariosProfissionais
                    .OrderBy(h => h.DiaSemana)
                    .ThenBy(h => h.HoraInicio)
                    .Select(h => new
                    {
                        diaSemana = h.DiaSemana,
                        horaInicio = h.HoraInicio.ToString(),
                        horaFim = h.HoraFim.ToString()
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (profissional == null)
        {
            return NotFound(new
            {
                mensagem = "Profissional não encontrada."
            });
        }

        return Ok(profissional);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Criar(
        [FromForm] ProfissionalFormulario dados)
    {
        var erro = Validar(dados);

        if (erro != null)
            return BadRequest(new { mensagem = erro });

        var horariosValidados = ValidarHorarios(
            dados.Horarios,
            out var erroHorarios);

        if (erroHorarios != null)
            return BadRequest(new { mensagem = erroHorarios });

        if (!string.IsNullOrWhiteSpace(dados.CPF) &&
            await _context.Profissionais.AnyAsync(
                p => p.CPF == dados.CPF))
        {
            return Conflict(new
            {
                mensagem = "Este CPF já está cadastrado."
            });
        }

        if (!string.IsNullOrWhiteSpace(dados.Telefone) &&
            await _context.Profissionais.AnyAsync(
                p => p.Telefone == dados.Telefone))
        {
            return Conflict(new
            {
                mensagem = "Este telefone já está cadastrado."
            });
        }

        var nome = dados.Nome.Trim();

        var profissional = new Profissional
        {
            Nome = nome,
            Profissao = dados.Profissao.Trim(),
            CPF = Limpar(dados.CPF),
            Telefone = Limpar(dados.Telefone),
            Endereco = Limpar(dados.Endereco),
            TaxaAtendimento = dados.TaxaAtendimento,
            Ativo = true
        };

        var servicosIds = dados.ServicoIds
            .Distinct()
            .ToList();

        var servicosValidos = await _context.Servicos
            .Where(s => servicosIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (servicosValidos.Count != servicosIds.Count)
        {
            return BadRequest(new
            {
                mensagem = "Um ou mais serviços selecionados não existem."
            });
        }

        var email = await GerarEmail(nome);

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            Senha = string.IsNullOrWhiteSpace(dados.Senha)
                ? "123456"
                : dados.Senha,
            TipoUsuario = "Funcionario",
            Profissional = profissional
        };

        await using var transacao =
            await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Profissionais.Add(profissional);
            _context.Usuarios.Add(usuario);

            foreach (var servicoId in servicosIds)
            {
                _context.ServicoProfissionais.Add(
                    new ServicoProfissional
                    {
                        Profissional = profissional,
                        ServicoId = servicoId
                    });
            }

            foreach (var horario in horariosValidados)
            {
                horario.Profissional = profissional;

                _context.HorariosProfissionais.Add(horario);
            }

            // Salva primeiro para obter o ID da profissional.
            await _context.SaveChangesAsync();

            // Envia a foto ao Cloudinary.
            if (dados.Foto != null)
            {
                var urlFoto = await SalvarFoto(
                    dados.Foto,
                    profissional.Id
                );

                profissional.Foto = urlFoto;

                await _context.SaveChangesAsync();
            }

            await transacao.CommitAsync();

            return CreatedAtAction(
                nameof(GetPorId),
                new { id = profissional.Id },
                new
                {
                    mensagem = "Profissional cadastrada com sucesso.",
                    id = profissional.Id,
                    email = usuario.Email,
                    imagem = profissional.Foto
                });
        }
        catch (Exception erroCadastro)
        {
            await transacao.RollbackAsync();

            _logger.LogError(
                erroCadastro,
                "Erro ao cadastrar profissional."
            );

            return StatusCode(500, new
            {
                mensagem = "Não foi possível cadastrar a profissional: " +
                           erroCadastro.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromForm] ProfissionalFormulario dados)
    {
        var profissional = await _context.Profissionais
            .Include(p => p.ServicosProfissionais)
            .Include(p => p.HorariosProfissionais)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profissional == null)
        {
            return NotFound(new
            {
                mensagem = "Profissional não encontrada."
            });
        }

        var erro = Validar(dados);

        if (erro != null)
            return BadRequest(new { mensagem = erro });

        var horariosValidados = ValidarHorarios(
            dados.Horarios,
            out var erroHorarios);

        if (erroHorarios != null)
            return BadRequest(new { mensagem = erroHorarios });

        if (!string.IsNullOrWhiteSpace(dados.CPF) &&
            await _context.Profissionais.AnyAsync(
                p => p.Id != id && p.CPF == dados.CPF))
        {
            return Conflict(new
            {
                mensagem = "Este CPF já está cadastrado."
            });
        }

        if (!string.IsNullOrWhiteSpace(dados.Telefone) &&
            await _context.Profissionais.AnyAsync(
                p => p.Id != id && p.Telefone == dados.Telefone))
        {
            return Conflict(new
            {
                mensagem = "Este telefone já está cadastrado."
            });
        }

        var servicosIds = dados.ServicoIds
            .Distinct()
            .ToList();

        var servicosValidos = await _context.Servicos
            .Where(s => servicosIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (servicosValidos.Count != servicosIds.Count)
        {
            return BadRequest(new
            {
                mensagem = "Um ou mais serviços não existem."
            });
        }

        string? novaFoto = null;

        if (dados.Foto != null)
        {
            try
            {
                novaFoto = await SalvarFoto(
                    dados.Foto,
                    profissional.Id
                );
            }
            catch (Exception erroFoto)
            {
                _logger.LogError(
                    erroFoto,
                    "Erro ao atualizar foto da profissional {Id}.",
                    id
                );

                return StatusCode(500, new
                {
                    mensagem = "Não foi possível enviar a foto ao Cloudinary: " +
                               erroFoto.Message
                });
            }
        }

        profissional.Nome = dados.Nome.Trim();
        profissional.Profissao = dados.Profissao.Trim();
        profissional.CPF = Limpar(dados.CPF);
        profissional.Telefone = Limpar(dados.Telefone);
        profissional.Endereco = Limpar(dados.Endereco);
        profissional.TaxaAtendimento = dados.TaxaAtendimento;

        if (novaFoto != null)
            profissional.Foto = novaFoto;

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.ProfissionalId == id);

        if (usuario != null)
        {
            usuario.Nome = profissional.Nome;

            if (!string.IsNullOrWhiteSpace(dados.Senha))
                usuario.Senha = dados.Senha;
        }
        else
        {
            var email = await GerarEmail(profissional.Nome);

            _context.Usuarios.Add(new Usuario
            {
                Nome = profissional.Nome,
                Email = email,
                Senha = string.IsNullOrWhiteSpace(dados.Senha)
                    ? "123456"
                    : dados.Senha,
                TipoUsuario = "Funcionario",
                ProfissionalId = profissional.Id
            });
        }

        _context.ServicoProfissionais.RemoveRange(
            profissional.ServicosProfissionais);

        foreach (var servicoId in servicosIds)
        {
            _context.ServicoProfissionais.Add(
                new ServicoProfissional
                {
                    ProfissionalId = profissional.Id,
                    ServicoId = servicoId
                });
        }

        _context.HorariosProfissionais.RemoveRange(
            profissional.HorariosProfissionais);

        foreach (var horario in horariosValidados)
        {
            horario.ProfissionalId = profissional.Id;

            _context.HorariosProfissionais.Add(horario);
        }

        try
        {
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Profissional atualizada com sucesso.",
                imagem = profissional.Foto
            });
        }
        catch (Exception erroAtualizacao)
        {
            _logger.LogError(
                erroAtualizacao,
                "Erro ao atualizar profissional {Id}.",
                id
            );

            return StatusCode(500, new
            {
                mensagem = "Não foi possível atualizar a profissional."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var profissional = await _context.Profissionais
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profissional == null)
        {
            return NotFound(new
            {
                mensagem = "Profissional não encontrada."
            });
        }

        profissional.Ativo = false;

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.ProfissionalId == id);

        if (usuario != null)
            _context.Usuarios.Remove(usuario);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Profissional desativada. O histórico de agendamentos foi preservado."
        });
    }

    [HttpGet("por-servicos")]
    public async Task<IActionResult> PorServicos(
        [FromQuery] List<int> servicoIds)
    {
        var ids = servicoIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return Ok(new List<object>());

        var profissionais = await _context.Profissionais
            .Where(p => p.Ativo)
            .Where(p => p.ServicosProfissionais
                .Select(sp => sp.ServicoId)
                .Distinct()
                .Count(id => ids.Contains(id)) == ids.Count)
            .Select(p => new
            {
                id = p.Id,
                nome = p.Nome,
                profissao = p.Profissao,
                imagem = p.Foto
            })
            .ToListAsync();

        return Ok(profissionais);
    }

    [HttpGet("painel")]
    public async Task<IActionResult> Painel()
    {
        var profissionalId =
            HttpContext.Session.GetInt32("ProfissionalId");

        if (profissionalId == null)
        {
            return Unauthorized(new
            {
                mensagem = "Profissional não está logada."
            });
        }

        var profissional = await _context.Profissionais
            .FirstOrDefaultAsync(p =>
                p.Id == profissionalId.Value && p.Ativo);

        if (profissional == null)
        {
            HttpContext.Session.Remove("ProfissionalId");

            return NotFound(new
            {
                mensagem = "Profissional não encontrada ou inativa."
            });
        }

        var horarios = await _context.HorariosProfissionais
            .Where(h => h.ProfissionalId == profissional.Id)
            .OrderBy(h => h.DiaSemana)
            .ThenBy(h => h.HoraInicio)
            .Select(h => new
            {
                diaSemana = h.DiaSemana,
                horaInicio = h.HoraInicio.ToString(),
                horaFim = h.HoraFim.ToString()
            })
            .ToListAsync();

        var agendamentos = await _context.Agendamentos
            .Include(a => a.Cliente)
            .Include(a => a.Servico)
            .Include(a => a.Combo)
                .ThenInclude(c => c.ComboServicos)
                    .ThenInclude(cs => cs.Servico)
            .Where(a =>
                a.ProfissionalId == profissional.Id &&
                a.Status != "Cancelado")
            .OrderBy(a => a.DataHora)
            .ToListAsync();

        var listaAgendamentos = agendamentos.Select(a =>
        {
            var atendimento = a.Combo != null
                ? a.Combo.Nome
                : a.Servico?.Nome ?? "Atendimento";

            var preco = a.Combo != null
                ? a.Combo.Preco
                : a.Servico?.Preco ?? 0;

            var duracao = a.Servico != null
                ? a.Servico.DuracaoMinutos
                : a.Combo != null
                    ? a.Combo.Nome.Equals(
                        "Belleza completa",
                        StringComparison.OrdinalIgnoreCase)
                        ? 240
                        : a.Combo.ComboServicos.Sum(
                            cs => cs.Servico.DuracaoMinutos)
                    : 0;

            return new
            {
                id = a.Id,
                dataHora = a.DataHora,
                cliente = a.Cliente?.Nome ?? "Cliente",
                atendimento,
                tipo = a.Combo != null ? "Combo" : "Serviço",
                preco,
                duracaoMinutos = duracao,
                status = a.Status
            };
        }).ToList();

        return Ok(new
        {
            profissional = new
            {
                id = profissional.Id,
                nome = profissional.Nome,
                profissao = profissional.Profissao,
                imagem = profissional.Foto
            },
            horariosTrabalho = horarios,
            agendamentos = listaAgendamentos
        });
    }

    private string? Validar(ProfissionalFormulario dados)
    {
        if (string.IsNullOrWhiteSpace(dados.Nome))
            return "Informe o nome da profissional.";

        if (dados.Nome.Trim().Length > 100)
            return "O nome deve ter no máximo 100 caracteres.";

        if (string.IsNullOrWhiteSpace(dados.Profissao))
            return "Informe a profissão.";

        if (dados.Profissao.Trim().Length > 100)
            return "A profissão deve ter no máximo 100 caracteres.";

        if (dados.TaxaAtendimento < 0)
            return "A taxa de atendimento não pode ser negativa.";

        if (!string.IsNullOrWhiteSpace(dados.CPF))
        {
            var cpf = new string(
                dados.CPF.Where(char.IsDigit).ToArray());

            if (cpf.Length != 11)
                return "O CPF deve conter 11 números.";

            dados.CPF = cpf;
        }

        if (!string.IsNullOrWhiteSpace(dados.Telefone))
        {
            var telefone = new string(
                dados.Telefone.Where(char.IsDigit).ToArray());

            if (telefone.Length < 10 || telefone.Length > 13)
                return "Informe um telefone válido.";

            dados.Telefone = telefone;
        }

        return null;
    }

    private static List<HorarioProfissional> ValidarHorarios(
        List<HorarioFormulario>? horarios,
        out string? erro)
    {
        erro = null;

        var resultado = new List<HorarioProfissional>();

        if (horarios == null || horarios.Count == 0)
        {
            erro = "Informe pelo menos um dia e horário de trabalho.";
            return resultado;
        }

        foreach (var item in horarios)
        {
            if (item.DiaSemana < 0 || item.DiaSemana > 6)
            {
                erro = "Um dos dias da semana informados é inválido.";
                return new List<HorarioProfissional>();
            }

            if (!TimeSpan.TryParse(
                    item.HoraInicio,
                    CultureInfo.InvariantCulture,
                    out var inicio) ||
                !TimeSpan.TryParse(
                    item.HoraFim,
                    CultureInfo.InvariantCulture,
                    out var fim))
            {
                erro = "Informe horários válidos para todos os períodos selecionados.";
                return new List<HorarioProfissional>();
            }

            if (inicio < TimeSpan.Zero ||
                fim > TimeSpan.FromDays(1) ||
                inicio >= fim)
            {
                erro = "O horário final deve ser maior que o horário inicial.";
                return new List<HorarioProfissional>();
            }

            resultado.Add(new HorarioProfissional
            {
                DiaSemana = (DayOfWeek)item.DiaSemana,
                HoraInicio = inicio,
                HoraFim = fim
            });
        }

        foreach (var grupo in resultado.GroupBy(h => h.DiaSemana))
        {
            var ordenados = grupo
                .OrderBy(h => h.HoraInicio)
                .ToList();

            for (int i = 1; i < ordenados.Count; i++)
            {
                if (ordenados[i].HoraInicio <
                    ordenados[i - 1].HoraFim)
                {
                    erro = $"Os períodos de {grupo.Key} não podem se sobrepor.";
                    return new List<HorarioProfissional>();
                }
            }
        }

        return resultado;
    }

    private async Task<string> SalvarFoto(
        IFormFile foto,
        int profissionalId)
    {
        if (foto == null ||
            foto.Length == 0 ||
            foto.Length > 5 * 1024 * 1024)
        {
            throw new InvalidOperationException(
                "Foto inválida. Envie JPG, PNG ou WEBP de até 5 MB."
            );
        }

        var extensao = Path.GetExtension(foto.FileName)
            .ToLowerInvariant();

        var extensoesPermitidas = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        if (!extensoesPermitidas.Contains(extensao))
        {
            throw new InvalidOperationException(
                "Formato de foto inválido. Envie JPG, PNG ou WEBP."
            );
        }

        await using var stream = foto.OpenReadStream();

        var parametros = new ImageUploadParams
        {
            File = new FileDescription(
                foto.FileName,
                stream
            ),
            PublicId = $"belleza/profissionais/profissional_{profissionalId}",
            Overwrite = true,
            Invalidate = true
        };

        var resultado = await _cloudinary.UploadAsync(parametros);

        if (resultado.Error != null)
        {
            throw new InvalidOperationException(
                resultado.Error.Message
            );
        }

        return resultado.SecureUrl.ToString();
    }

    private async Task<string> GerarEmail(string nome)
    {
        var baseNome = RemoverAcentos(nome)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

        baseNome = new string(baseNome
            .Where(char.IsLetterOrDigit)
            .ToArray());

        if (string.IsNullOrWhiteSpace(baseNome))
            baseNome = "profissional";

        for (var tentativa = 0; tentativa < 30; tentativa++)
        {
            var sufixo = tentativa == 0
                ? ""
                : Random.Shared.Next(1, 101).ToString();

            var email = $"{baseNome}{sufixo}@beleza.com";

            if (!await _context.Usuarios.AnyAsync(
                u => u.Email.ToLower() == email.ToLower()))
            {
                return email;
            }
        }

        var contador = 1;

        while (await _context.Usuarios.AnyAsync(
            u => u.Email.ToLower() ==
                 $"{baseNome}{contador}@beleza.com".ToLower()))
        {
            contador++;
        }

        return $"{baseNome}{contador}@beleza.com";
    }

    private static string? Limpar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }

    private static string RemoverAcentos(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder();

        foreach (var caractere in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) !=
                UnicodeCategory.NonSpacingMark)
            {
                resultado.Append(caractere);
            }
        }

        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }
}
