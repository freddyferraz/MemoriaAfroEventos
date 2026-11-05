using TechFlow.Eventos.Core.Enums;

namespace TechFlow.Eventos.Core.Models;

/// <summary>Filtros da tela "Explorar eventos".</summary>
public class EventoFiltro
{
    public string? Texto { get; set; }
    public string? Cidade { get; set; }
    public CategoriaEvento? Categoria { get; set; }
    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }
    public decimal? PrecoMax { get; set; }
    public bool SomenteGratuitos { get; set; }
    public bool SomenteDestaques { get; set; }

    // Distância: posição do visitante + raio em km.
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? DistanciaKm { get; set; }
}
