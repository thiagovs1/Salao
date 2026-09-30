using SalaoBeleza.DTOs;
using SalaoBeleza.Models;
using SalaoBeleza.Repositories;

namespace SalaoBeleza.Services;

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

    public async Task<ClienteRespostaDTO> Criar(ClienteDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new Exception("O nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new Exception("O e-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Telefone))
            throw new Exception("O telefone é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Senha))
            throw new Exception("A senha é obrigatória.");

        var clienteExistente = await _repo.BuscarPorEmail(dto.Email);

        if (clienteExistente != null)
            throw new Exception("Este e-mail já está cadastrado.");

        var cliente = new Cliente
        {
            Nome = dto.Nome,
            Telefone = dto.Telefone,
            Email = dto.Email,
            Senha = dto.Senha,
            Cpf = dto.Cpf
        };

        var criado = await _repo.Adicionar(cliente);

        return new ClienteRespostaDTO(
            criado.Id,
            criado.Nome,
            criado.Telefone,
            criado.Email
        );
    }

    public async Task<ClienteRespostaDTO?> Login(string email, string senha)
    {
        var cliente = await _repo.BuscarPorEmail(email);

        if (cliente == null)
            return null;

        if (cliente.Senha != senha)
            return null;

        return new ClienteRespostaDTO(
            cliente.Id,
            cliente.Nome,
            cliente.Telefone,
            cliente.Email
        );
    }

    public async Task<bool> Atualizar(int id, ClienteDTO dto)
    {
        var cliente = await _repo.BuscarPorId(id);

        if (cliente == null)
            return false;

        cliente.Nome = dto.Nome;
        cliente.Telefone = dto.Telefone;
        cliente.Email = dto.Email;
        cliente.Senha = dto.Senha;
        cliente.Cpf = dto.Cpf;

        await _repo.Atualizar(cliente);

        return true;
    }

    public async Task<bool> Remover(int id)
    {
        var cliente = await _repo.BuscarPorId(id);

        if (cliente == null)
            return false;

        await _repo.Remover(cliente);

        return true;
    }
}