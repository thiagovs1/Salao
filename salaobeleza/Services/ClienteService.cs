
using SalaoBeleza.DTOs;
using SalaoBeleza.Models;
using SalaoBeleza.Repositories;

namespace SalaoBeleza.Services;

// Contem as regras de negocio e converte entre DTO e entidade.
public class ClienteService
{
    private readonly IClienteRepository _repo;

    public ClienteService(IClienteRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<ClienteRespostaDTO>> Listar()
    {
        var clientes = await _repo.ListarTodos();

        return clientes
            .Select(c => new ClienteRespostaDTO(
                c.Id,
                c.Nome,
                c.Telefone,
                c.Email
            ))
            .ToList();
    }

    public async Task<ClienteRespostaDTO?> BuscarPorId(int id)
    {
        var c = await _repo.BuscarPorId(id);

        if (c == null)
            return null;

        return new ClienteRespostaDTO(
            c.Id,
            c.Nome,
            c.Telefone,
            c.Email
        );
    }

    public async Task<ClienteRespostaDTO> Criar(
        ClienteCriarDTO dto)
    {
        // Regra: nome nao pode ser vazio.
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new Exception(
                "O nome e obrigatorio."
            );

        var cliente = new Cliente
        {
            Nome = dto.Nome,
            Telefone = dto.Telefone,
            Email = dto.Email
        };

        var criado =
            await _repo.Adicionar(cliente);

        return new ClienteRespostaDTO(
            criado.Id,
            criado.Nome,
            criado.Telefone,
            criado.Email
        );
    }

    public async Task<bool> Atualizar(
        int id,
        ClienteCriarDTO dto)
    {
        var cliente =
            await _repo.BuscarPorId(id);

        if (cliente == null)
            return false;

        cliente.Nome = dto.Nome;
        cliente.Telefone = dto.Telefone;
        cliente.Email = dto.Email;

        await _repo.Atualizar(cliente);

        return true;
    }

    public async Task<bool> Remover(int id)
    {
        var cliente =
            await _repo.BuscarPorId(id);

        if (cliente == null)
            return false;

        await _repo.Remover(cliente);

        return true;
    }
}

