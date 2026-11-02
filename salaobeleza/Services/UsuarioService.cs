
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

    private const string DominioAdmin = "@adm.belleza.com";

    private const string EmailAdminPrincipal =
        "Administrador@adm.Belleza.com";

    public UsuarioService(
        IUsuarioRepository usuarioRepo,
        IClienteRepository clienteRepo)
    {
        _usuarioRepo = usuarioRepo;
        _clienteRepo = clienteRepo;
    }

    private static bool EhEmailAdministrativo(string email)
    {
        return email.Trim().EndsWith(
            DominioAdmin,
            StringComparison.OrdinalIgnoreCase);
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

        // Impede cadastro público de qualquer e-mail administrativo.
        if (EhEmailAdministrativo(email))
        {
            throw new Exception(
                "Este domínio de e-mail é reservado aos administradores.");
        }

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

    // Chamado exclusivamente pelo endpoint protegido do administrador principal.
    public async Task<Usuario> CriarAdministrador(
        UsuarioCriarDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new Exception("O nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new Exception("O e-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Senha))
            throw new Exception("A senha é obrigatória.");

        var email = dto.Email.Trim();

        if (EhEmailAdministrativo(email))
        {
            throw new Exception(
                "O domínio @adm.Belleza.com é reservado ao administrador principal.");
        }

        if (string.Equals(
            email,
            EmailAdminPrincipal,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "Este e-mail pertence ao administrador principal.");
        }

        var usuarioExistente =
            await _usuarioRepo.BuscarPorEmail(email);

        if (usuarioExistente != null)
            throw new Exception("Este e-mail já está cadastrado.");

        var clienteExistente =
            await _clienteRepo.BuscarPorEmail(email);

        if (clienteExistente != null)
            throw new Exception(
                "Este e-mail já pertence a uma conta de cliente.");

        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            TipoUsuario = "Administrador",
            ProfissionalId = null
        };

        usuario.Senha = _passwordHasher.HashPassword(
            usuario,
            dto.Senha
        );

        return await _usuarioRepo.Adicionar(usuario);
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

        // Nunca permite autenticação de uma conta falsa
        // usando o endereço reservado ao administrador principal.
        if (string.Equals(
            usuario.Email,
            EmailAdminPrincipal,
            StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Compatibilidade com senhas antigas em texto puro.
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

        if (EhEmailAdministrativo(email))
        {
            throw new Exception(
                "A recuperação pública de senhas administrativas não está disponível.");
        }

        var usuario = await _usuarioRepo.BuscarPorEmail(email);

        if (usuario == null)
            throw new Exception("Não existe uma conta com este e-mail.");

        if (string.Equals(
            usuario.TipoUsuario,
            "Administrador",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "A senha de administradores não pode ser alterada pela recuperação pública.");
        }

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
