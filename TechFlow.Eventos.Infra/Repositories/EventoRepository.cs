using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Geo;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Models;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Infra.Repositories;

public class EventoRepository : IEventoRepository
{
    private readonly AppDbContext _db;

    public EventoRepository(AppDbContext db) => _db = db;

    public Task<Evento?> ObterPorIdAsync(int id) => _db.Eventos.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IReadOnlyList<Evento>> BuscarAprovadosAsync(EventoFiltro f)
    {
        var de = f.De ?? Evento.HojeBrasil();
        var query = _db.Eventos.AsNoTracking().Where(e => e.Status == StatusEvento.Aprovado && e.Data >= de);

        if (f.Ate is { } ate) query = query.Where(e => e.Data <= ate);
        if (!string.IsNullOrWhiteSpace(f.Cidade)) query = query.Where(e => e.Cidade == f.Cidade);
        if (f.Categoria is { } cat) query = query.Where(e => e.Categoria == cat);
        if (f.SomenteDestaques) query = query.Where(e => e.Destaque);
        if (f.SomenteGratuitos) query = query.Where(e => e.Preco <= 0);
        else if (f.PrecoMax is { } max) query = query.Where(e => e.Preco <= max);

        if (!string.IsNullOrWhiteSpace(f.Texto))
        {
            var t = f.Texto.Trim().ToLower();
            query = query.Where(e => e.Titulo.ToLower().Contains(t)
                                  || e.Atracoes.ToLower().Contains(t)
                                  || e.Organizador.ToLower().Contains(t));
        }

        var lista = await query.OrderBy(e => e.Data).ThenBy(e => e.Horario).ToListAsync();

        // Distância em memória (volume pequeno no MVP). Eventos sem coordenadas não têm como
        // ser medidos, então permanecem na lista.
        if (f.Latitude is { } lat && f.Longitude is { } lng && f.DistanciaKm is { } raio)
        {
            var origem = new Coordenadas(lat, lng);
            lista = lista.Where(e => e.Latitude is null || e.Longitude is null
                                  || origem.DistanciaKm(new Coordenadas(e.Latitude.Value, e.Longitude.Value)) <= raio)
                         .ToList();
        }

        return lista;
    }

    public async Task<IReadOnlyList<Evento>> ListarPorStatusAsync(StatusEvento status) =>
        await _db.Eventos.AsNoTracking().Where(e => e.Status == status).OrderBy(e => e.DataEnvio).ToListAsync();

    public async Task<IReadOnlyList<string>> ListarCidadesAprovadasAsync() =>
        await _db.Eventos.Where(e => e.Status == StatusEvento.Aprovado)
                         .Select(e => e.Cidade).Distinct().OrderBy(c => c).ToListAsync();

    public Task<EventoFoto?> ObterFotoAsync(int eventoId) =>
        _db.Fotos.AsNoTracking().FirstOrDefaultAsync(f => f.EventoId == eventoId);

    public async Task AdicionarAsync(Evento evento) => await _db.Eventos.AddAsync(evento);

    public Task SalvarAlteracoesAsync() => _db.SaveChangesAsync();
}
