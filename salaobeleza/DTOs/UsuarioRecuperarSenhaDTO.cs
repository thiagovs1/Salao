using System.ComponentModel.DataAnnotations;

namespace SalaoBeleza.DTOs;

public class UsuarioRecuperarSenhaDTO
{
[Required]
public string Email { get; set; } = string.Empty;


[Required]
public string NovaSenha { get; set; } = string.Empty;

[Required]
public string ConfirmarSenha { get; set; } = string.Empty;


}
