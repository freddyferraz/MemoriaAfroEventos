using System.ComponentModel.DataAnnotations;

namespace TechFlow.Eventos.Api.DTOs;

public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Senha { get; set; } = string.Empty;
}

public class NovoModeradorDto
{
    [Required, MaxLength(150)] public string NomeCompleto { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Senha { get; set; } = string.Empty;
}

public class RegistrarDto
{
    [Required, MaxLength(150)] public string NomeCompleto { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8), MaxLength(100)] public string Senha { get; set; } = string.Empty;

    /// <summary>Concordância com o uso dos dados da conta. Obrigatório.</summary>
    public bool AceiteTermos { get; set; }
}

public class ModerarCadastroDto
{
    public bool Aprovar { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}
