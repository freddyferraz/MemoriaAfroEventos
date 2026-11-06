using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Core.Interfaces;

public interface IEventoRepository
{
    Task<Evento?> ObterPorIdAsync(int id);

    /// <summary>Eventos aprovados, de hoje em diante (ou a partir de Filtro.De), em ordem cronológica.</summary>
    Task<IReadOnlyList<Evento>> BuscarAprovadosAsync(EventoFiltro filtro);

    Task<IReadOnlyList<Evento>> ListarPorStatusAsync(StatusEvento status);
    Task<IReadOnlyList<string>> ListarCidadesAprovadasAsync();
    Task<EventoFoto?> ObterFotoAsync(int eventoId);
    Task AdicionarAsync(Evento evento);
    Task SalvarAlteracoesAsync();
}
