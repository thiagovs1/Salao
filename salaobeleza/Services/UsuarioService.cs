
using Microsoft.AspNetCore.Identity;
using SalaoBeleza.DTOs;
using SalaoBeleza.Models;
using SalaoBeleza.Repositories;

namespace SalaoBeleza.Services;

public class UsuarioService
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IClienteRepository _clienteRepo;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

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
            await _clienteRepo.BuscarPorTelefone(dto.Telefone.Trim());

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
            TipoUsuario = "Cliente",
            ProfissionalId = null
        };

        usuario.Senha = _passwordHasher.HashPassword(
            usuario,
            dto.Senha
        );

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

        // Primeiro verifica se a senha antiga está em texto puro.
        if (usuario.Senha == dto.Senha)
        {
            var novoHash = _passwordHasher.HashPassword(
                usuario,
                dto.Senha
            );

            await _usuarioRepo.AlterarSenha(
                usuario.Email,
                novoHash
            );

            usuario.Senha = novoHash;

            return usuario;
        }

        // Depois verifica a senha criptografada.
        PasswordVerificationResult resultado;

        try
        {
            resultado = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.Senha,
                dto.Senha
            );
        }
        catch (FormatException)
        {
            // Senha armazenada em formato inválido.
            return null;
        }

        if (resultado == PasswordVerificationResult.Failed)
            return null;

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var novoHash = _passwordHasher.HashPassword(
                usuario,
                dto.Senha
            );

            await _usuarioRepo.AlterarSenha(
                usuario.Email,
                novoHash
            );

            usuario.Senha = novoHash;
        }

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

        var email = dto.Email.Trim();

        var usuario = await _usuarioRepo.BuscarPorEmail(email);

        if (usuario == null)
            throw new Exception("Não existe uma conta com este e-mail.");

        var novoHash = _passwordHasher.HashPassword(
            usuario,
            dto.NovaSenha
        );

        return await _usuarioRepo.AlterarSenha(
            email,
            novoHash
        );
    }
}
