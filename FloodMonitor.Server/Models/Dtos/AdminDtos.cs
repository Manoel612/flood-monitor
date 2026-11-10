using System.ComponentModel.DataAnnotations;

namespace FloodMonitor.Server.Models.Dtos;

public class AdminLoginForm
{
    [Required(ErrorMessage = "Informe o usuário."), StringLength(100)]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Informe a senha."), StringLength(200)]
    public string Password { get; set; } = "";
}

public record AdminLoginResultDto(string Token);
