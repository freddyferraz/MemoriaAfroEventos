using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Core.Recomendacoes;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Infra.Recomendacoes;

public class PreferenciasRepository : IPreferenciasRepository
{
    private readonly AppDbContext _db;

    public PreferenciasRepository(AppDbContext db) => _db = db;

    public Task<PreferenciasUsuario?> ObterAsync(string usuarioId) =>
        _db.Preferencias.AsNoTracking().FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

    public async Task SalvarAsync(string usuarioId, int[] categorias, string[] cidades)
    {
        var atual = await _db.Preferencias.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);
        if (atual is null)
        {
            _db.Preferencias.Add(new PreferenciasUsuario { UsuarioId = usuarioId, Categorias = categorias, Cidades = cidades });
        }
        else
        {
            atual.Categorias = categorias;
            atual.Cidades = cidades;
            atual.AtualizadoEm = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}
