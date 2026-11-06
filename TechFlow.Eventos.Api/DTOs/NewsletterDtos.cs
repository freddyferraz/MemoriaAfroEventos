using System.ComponentModel.DataAnnotations;

namespace TechFlow.Eventos.Api.DTOs;

public class AssinarNewsletterDto
{
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = string.Empty;

    /// <summary>Valores de CategoriaEvento. Vazio = todas.</summary>
    public int[]? Categorias { get; set; }

    /// <summary>Nomes de cidades. Vazio = todas.</summary>
    public string[]? Cidades { get; set; }

    /// <summary>Aceite do consentimento (LGPD). Obrigatório.</summary>
    public bool AceiteLgpd { get; set; }
}

public class TokenNewsletterDto
{
    [Required, MaxLength(64)] public string Token { get; set; } = string.Empty;
}
