using Microsoft.AspNetCore.Identity;
using TechFlow.Eventos.Core.Enums;

namespace TechFlow.Eventos.Infra.Data;

/// <summary>Conta de usuário (administrador, moderador ou cliente). Quem só consulta ou divulga eventos não precisa de conta.</summary>
public class ApplicationUser : IdentityUser
{
    public string NomeCompleto { get; set; } = string.Empty;

    /// <summary>Contas novas nascem Pendente; o login só é liberado depois que um moderador aprova.</summary>
    public StatusCadastro Status { get; set; } = StatusCadastro.Aprovado;

    public DateTime? DataCadastro { get; set; }
}
