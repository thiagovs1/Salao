using SalaoBeleza.DTOs;
using SalaoBeleza.Models;
using SalaoBeleza.Repositories;

namespace SalaoBeleza.Services;

public class ServicoService
{
    private readonly IServicoRepository _repo;

    public ServicoService(IServicoRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<ServicoRespostaDTO>> Listar()
    {
        var servicos = await _repo.ListarTodos();
        return servicos.Select(Converter).ToList();
    }

    public async Task<ServicoRespostaDTO?> BuscarPorId(int id)
    {
        var servico = await _repo.BuscarPorId(id);
        return servico == null ? null : Converter(servico);
    }

    public async Task<ServicoRespostaDTO> Criar(ServicoCriarDTO dto)
    {
        Validar(dto);

        var ids = NormalizarIds(dto.ProfissionalIds);
        await ValidarProfissionais(ids);

        var servico = new Servico
        {
            Nome = dto.Nome.Trim(),
            Preco = dto.Preco,
            DuracaoMinutos = dto.DuracaoMinutos,
            Profissionais = ids.Select(id => new ServicoProfissional
            {
                ProfissionalId = id
            }).ToList()
        };

        var criado = await _repo.Adicionar(servico);
        var completo = await _repo.BuscarPorId(criado.Id);

        return Converter(completo ?? criado);
    }

    public async Task<ServicoRespostaDTO?> Atualizar(
        int id,
        ServicoCriarDTO dto)
    {
        Validar(dto);

        var servico = await _repo.BuscarPorId(id);

        if (servico == null)
            return null;

        var ids = NormalizarIds(dto.ProfissionalIds);
        await ValidarProfissionais(ids);

        servico.Nome = dto.Nome.Trim();
        servico.Preco = dto.Preco;
        servico.DuracaoMinutos = dto.DuracaoMinutos;

        var idsSelecionados = ids.ToHashSet();

        var vinculosRemover = servico.Profissionais
            .Where(sp => !idsSelecionados.Contains(sp.ProfissionalId))
            .ToList();

        foreach (var vinculo in vinculosRemover)
        {
            servico.Profissionais.Remove(vinculo);
        }

        var idsExistentes = servico.Profissionais
            .Select(sp => sp.ProfissionalId)
            .ToHashSet();

        foreach (var profissionalId in ids)
        {
            if (!idsExistentes.Contains(profissionalId))
            {
                servico.Profissionais.Add(new ServicoProfissional
                {
                    ServicoId = servico.Id,
                    ProfissionalId = profissionalId
                });
            }
        }

        await _repo.Atualizar(servico);

        var atualizado = await _repo.BuscarPorId(id);

        return atualizado == null ? null : Converter(atualizado);
    }

    public async Task<bool> Remover(int id)
    {
        var servico = await _repo.BuscarPorId(id);

        if (servico == null)
            return false;

        await _repo.Remover(servico);

        return true;
    }

    private async Task<List<Profissional>> ValidarProfissionais(
        List<int> ids)
    {
        var profissionais =
            await _repo.BuscarProfissionaisAtivosPorIds(ids);

        if (profissionais.Count != ids.Count)
        {
            throw new Exception(
                "Um ou mais profissionais não existem ou estão inativos."
            );
        }

        return profissionais;
    }

    private static List<int> NormalizarIds(List<int>? ids)
    {
        return (ids ?? new List<int>())
            .Distinct()
            .ToList();
    }

    private static void Validar(ServicoCriarDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new Exception("Informe o nome do serviço.");

        if (dto.Nome.Trim().Length > 100)
            throw new Exception(
                "O nome deve ter no máximo 100 caracteres."
            );

        if (dto.Preco <= 0)
            throw new Exception("O preço deve ser maior que zero.");

        if (dto.DuracaoMinutos <= 0)
            throw new Exception("A duração deve ser maior que zero.");
    }

    private static ServicoRespostaDTO Converter(Servico servico)
    {
        var profissionais = servico.Profissionais
            .Where(sp => sp.Profissional != null)
            .Select(sp => new ProfissionalServicoDTO(
                sp.Profissional!.Id,
                sp.Profissional.Nome
            ))
            .ToList();

        return new ServicoRespostaDTO(
            servico.Id,
            servico.Nome,
            servico.Preco,
            servico.DuracaoMinutos,
            profissionais
        );
    }
}
