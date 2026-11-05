using System.ComponentModel.DataAnnotations;
using TechFlow.Eventos.Core.Enums;

namespace TechFlow.Eventos.Api.DTOs;

/// <summary>Recebido do formulário "Divulgar evento" (organizadores e moradores).</summary>
public class EventoCreateDto
{
    [Required, MaxLength(200)] public string Titulo { get; set; } = string.Empty;
    [Required, EnumDataType(typeof(CategoriaEvento))] public CategoriaEvento Categoria { get; set; }
    [Required, MaxLength(120)] public string Cidade { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string Endereco { get; set; } = string.Empty;

    /// <summary>Opcional: link do Google Maps (as coordenadas são extraídas dele) ou Latitude + Longitude.</summary>
    [MaxLength(1000)] public string? LinkGoogleMaps { get; set; }
    [Range(-90, 90)] public double? Latitude { get; set; }
    [Range(-180, 180)] public double? Longitude { get; set; }

    [Required] public DateOnly Data { get; set; }
    [Required] public TimeOnly Horario { get; set; }

    [MaxLength(2000)] public string Atracoes { get; set; } = string.Empty;
    [Range(0, 100000)] public decimal Preco { get; set; }
    [MaxLength(500)] public string Acessibilidade { get; set; } = string.Empty;
    [MaxLength(500)] public string Ingressos { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Organizador { get; set; } = string.Empty;
    [MaxLength(500)] public string FonteOficial { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string ContatoEnvio { get; set; } = string.Empty;
    [EnumDataType(typeof(TipoAutorEvento))] public TipoAutorEvento TipoAutor { get; set; } = TipoAutorEvento.Morador;
}

public class EventoDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public CategoriaEvento Categoria { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateOnly Data { get; set; }
    public TimeOnly Horario { get; set; }
    public string Atracoes { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public bool Gratuito { get; set; }
    public string Acessibilidade { get; set; } = string.Empty;
    public string Ingressos { get; set; } = string.Empty;
    public string Organizador { get; set; } = string.Empty;
    public string FonteOficial { get; set; } = string.Empty;
    public bool Destaque { get; set; }
    public StatusEvento Status { get; set; }
    public TipoAutorEvento TipoAutor { get; set; }

    /// <summary>Preenchido apenas na lista de pendentes (moderadores).</summary>
    public string? ContatoEnvio { get; set; }

    /// <summary>Preenchido quando a busca informa a posição do visitante.</summary>
    public double? DistanciaKm { get; set; }
}

public class EventoModerarDto
{
    [Required, EnumDataType(typeof(StatusEvento))] public StatusEvento NovoStatus { get; set; }
    public bool Destaque { get; set; }
}
