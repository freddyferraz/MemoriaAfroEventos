using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Core.Newsletter;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Infra.Newsletter;

public class AssinanteRepository : IAssinanteRepository
{
    private readonly AppDbContext _db;

    public AssinanteRepository(AppDbContext db) => _db = db;

    public Task<Assinante?> ObterPorEmailAsync(string email) => _db.Assinantes.FirstOrDefaultAsync(a => a.Email == email);

    public Task<Assinante?> ObterPorTokenAsync(string token) => _db.Assinantes.FirstOrDefaultAsync(a => a.Token == token);

    public async Task<IReadOnlyList<Assinante>> ListarConfirmadosAsync() =>
        await _db.Assinantes.AsNoTracking().Where(a => a.Confirmado).ToListAsync();

    public async Task<HashSet<int>> ListarIdsJaNotificadosAsync(int eventoId) =>
        (await _db.EnviosNewsletter.Where(x => x.EventoId == eventoId).Select(x => x.AssinanteId).ToListAsync()).ToHashSet();

    public async Task AdicionarAsync(Assinante assinante) => await _db.Assinantes.AddAsync(assinante);

    public void Remover(Assinante assinante) => _db.Assinantes.Remove(assinante);

    public async Task RegistrarEnvioAsync(int eventoId, int assinanteId)
    {
        _db.EnviosNewsletter.Add(new EnvioNewsletter { EventoId = eventoId, AssinanteId = assinanteId });
        await _db.SaveChangesAsync();
    }

    public Task SalvarAlteracoesAsync() => _db.SaveChangesAsync();
}
