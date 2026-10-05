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
        return servicos
            .Select(s => new ServicoRespostaDTO(s.Id, s.Nome, s.Preco, s.DuracaoMinutos))
            .ToList();
    }

    public async Task<ServicoRespostaDTO?> BuscarPorId(int id)
    {
        var s = await _repo.BuscarPorId(id);
        if (s == null)
            return null;

        return new ServicoRespostaDTO(s.Id, s.Nome, s.Preco, s.DuracaoMinutos);
    }

    public async Task<ServicoRespostaDTO> Criar(ServicoCriarDTO dto)
    {
        // Regra: preco tem que ser maior que zero.
        if (dto.Preco <= 0)
            throw new Exception("O preco deve ser maior que zero.");

        var servico = new Servico
        {
            Nome = dto.Nome,
            Preco = dto.Preco,
            DuracaoMinutos = dto.DuracaoMinutos
        };

        var criado = await _repo.Adicionar(servico);
        return new ServicoRespostaDTO(criado.Id, criado.Nome, criado.Preco, criado.DuracaoMinutos);
    }

    public async Task<bool> Remover(int id)
    {
        var servico = await _repo.BuscarPorId(id);
        if (servico == null)
            return false;

        await _repo.Remover(servico);
        return true;
    }
}
