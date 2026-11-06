using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Core.Interfaces;

/// <summary>
/// Ponto de extensão do módulo de Eventos: quem quiser reagir à publicação de um evento
/// (newsletter, notificações push, integrações) implementa esta interface e registra no DI.
/// O módulo de Eventos não conhece quem escuta.
/// </summary>
public interface IEventoPublicadoHandler
{
    Task AoPublicarAsync(Evento evento, CancellationToken ct = default);
}
