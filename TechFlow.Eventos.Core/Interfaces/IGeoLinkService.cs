using TechFlow.Eventos.Core.Geo;

namespace TechFlow.Eventos.Core.Interfaces;

public interface IGeoLinkService
{
    /// <summary>Retorna as coordenadas de um link do Google Maps, ou null se não for possível extrair.</summary>
    Task<Coordenadas?> ExtrairCoordenadasAsync(string link, CancellationToken ct = default);
}
