using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Core.Avaliacoes;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Infra.Avaliacoes;

public class AvaliacaoRepository : IAvaliacaoRepository
{
    private readonly AppDbContext _db;

    public AvaliacaoRepository(AppDbContext db) => _db = db;

    public async Task AdicionarAsync(Avaliacao avaliacao) => await _db.Avaliacoes.AddAsync(avaliacao);

    public Task<Avaliacao?> ObterPorIdAsync(int id) => _db.Avaliacoes.FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IReadOnlyList<Avaliacao>> ListarAprovadasAsync(int eventoId, int limite) =>
        await _db.Avaliacoes.AsNoTracking()
            .Where(a => a.EventoId == eventoId && a.Status == StatusAvaliacao.Aprovada)
            .OrderByDescending(a => a.DataEnvio).Take(limite).ToListAsync();

    public async Task<IReadOnlyList<ResumoAvaliacoes>> ResumirAsync(IReadOnlyCollection<int> eventoIds)
    {
        var ids = eventoIds.Distinct().Take(100).ToArray();
        var linhas = await _db.Avaliacoes.AsNoTracking()
            .Where(a => ids.Contains(a.EventoId) && a.Status == StatusAvaliacao.Aprovada)
            .GroupBy(a => a.EventoId)
            .Select(g => new { EventoId = g.Key, Media = g.Average(x => (double)x.Nota), Total = g.Count() })
            .ToListAsync();
        return linhas.Select(l => new ResumoAvaliacoes(l.EventoId, Math.Round(l.Media, 1), l.Total)).ToList();
    }

    public async Task<IReadOnlyList<AvaliacaoPendente>> ListarPendentesAsync()
    {
        var linhas = await (from a in _db.Avaliacoes.AsNoTracking()
                            join e in _db.Eventos.AsNoTracking() on a.EventoId equals e.Id
                            where a.Status == StatusAvaliacao.Pendente
                            orderby a.DataEnvio
                            select new { Avaliacao = a, e.Titulo }).ToListAsync();
        return linhas.Select(l => new AvaliacaoPendente(l.Avaliacao, l.Titulo)).ToList();
    }

    public Task SalvarAlteracoesAsync() => _db.SaveChangesAsync();
}
