
using SalaoBeleza.DTOs;
using SalaoBeleza.Models;
using SalaoBeleza.Repositories;

namespace SalaoBeleza.Services;

public class UsuarioService
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IClienteRepository _clienteRepo;

    public UsuarioService(
        IUsuarioRepository usuarioRepo,
        IClienteRepository clienteRepo)
    {
        _usuarioRepo = usuarioRepo;
        _clienteRepo = clienteRepo;
    }

    public async Task<Usuario> Criar(UsuarioCriarDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new Exception("O nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new Exception("O e-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Telefone))
            throw new Exception("O telefone é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Senha))
            throw new Exception("A senha é obrigatória.");

        var email = dto.Email.Trim();

        var usuarioExistente =
            await _usuarioRepo.BuscarPorEmail(email);

        if (usuarioExistente != null)
            throw new Exception("Este e-mail já está cadastrado.");

        var clienteExistente =
            await _clienteRepo.BuscarPorTelefone(dto.Telefone);

        if (clienteExistente != null)
            throw new Exception("Este telefone já está cadastrado.");

        var clienteEmailExistente =
            await _clienteRepo.BuscarPorEmail(email);

        if (clienteEmailExistente != null)
            throw new Exception("Este e-mail já está cadastrado.");

        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            Senha = dto.Senha,
            TipoUsuario = "Cliente",
            ProfissionalId = null
        };

        var cliente = new Cliente
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            Telefone = dto.Telefone.Trim()
        };

        var usuarioCriado =
            await _usuarioRepo.Adicionar(usuario);

        await _clienteRepo.Adicionar(cliente);

        return usuarioCriado;
    }

    public async Task<Usuario?> Login(UsuarioLoginDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Senha))
        {
            return null;
        }

        var usuario = await _usuarioRepo.BuscarPorEmail(
            dto.Email.Trim()
        );

        if (usuario == null)
            return null;

        if (usuario.Senha != dto.Senha)
            return null;

        return usuario;
    }

    public async Task<Cliente?> BuscarClientePorEmail(string email)
    {
        return await _clienteRepo.BuscarPorEmail(email);
    }

    public async Task<bool> RecuperarSenha(
        UsuarioRecuperarSenhaDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new Exception("O e-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.NovaSenha))
            throw new Exception("A nova senha é obrigatória.");

        if (string.IsNullOrWhiteSpace(dto.ConfirmarSenha))
            throw new Exception("A confirmação da senha é obrigatória.");

        if (dto.NovaSenha != dto.ConfirmarSenha)
            throw new Exception("As senhas não são iguais.");

        var usuario = await _usuarioRepo.BuscarPorEmail(
            dto.Email.Trim()
        );

        if (usuario == null)
            throw new Exception("Não existe uma conta com este e-mail.");

        return await _usuarioRepo.AlterarSenha(
            dto.Email.Trim(),
            dto.NovaSenha
        );
    }
}
