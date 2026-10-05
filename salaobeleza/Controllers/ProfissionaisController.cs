using Microsoft.AspNetCore.Mvc;
using SalaoBeleza.Data;

namespace SalaoBeleza.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfissionaisController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfissionaisController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var profissionais = _context.Profissionais
                .Where(p => p.Ativo)
                .Select(p => new
                {
                    id = p.Id,
                    nome = p.Nome,
                    profissao = p.Profissao,
                    imagem = p.Foto
                })
                .ToList();

            return Ok(profissionais);
        }
    }
}