using Microsoft.AspNetCore.Identity;

namespace TechFlow.Eventos.Infra.Data;

/// <summary>Conta de moderador/administrador. Visitantes e organizadores não precisam de conta.</summary>
public class ApplicationUser : IdentityUser
{
    public string NomeCompleto { get; set; } = string.Empty;
}
