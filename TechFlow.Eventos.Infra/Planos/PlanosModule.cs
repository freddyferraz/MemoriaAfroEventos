using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechFlow.Eventos.Core.Pagamentos;
using TechFlow.Eventos.Core.Planos;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Infra.Planos;

public class AssinaturaPlanoRepository : IAssinaturaPlanoRepository
{
    private readonly AppDbContext _db;

    public AssinaturaPlanoRepository(AppDbContext db) => _db = db;

    public async Task AdicionarAsync(AssinaturaPlano assinatura) => await _db.AssinaturasPlano.AddAsync(assinatura);

    public Task<AssinaturaPlano?> ObterAsync(int id) => _db.AssinaturasPlano.FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IReadOnlyList<AssinaturaPlano>> ListarAtivasAsync(string usuarioId, DateTime agoraUtc) =>
        await _db.AssinaturasPlano.Where(a => a.UsuarioId == usuarioId && a.Status == StatusAssinatura.Ativa && a.ValidaAte > agoraUtc).ToListAsync();

    public Task SalvarAlteracoesAsync() => _db.SaveChangesAsync();
}

public static class PlanosModelConfig
{
    public static void Aplicar(ModelBuilder b)
    {
        b.Entity<AssinaturaPlano>(a =>
        {
            a.Property(x => x.UsuarioId).IsRequired();
            a.HasIndex(x => new { x.UsuarioId, x.Status });
            a.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public static class PlanosModule
{
    /// <summary>Registra o módulo Planos. O módulo de Eventos só enxerga a interface ICotaDestaque.</summary>
    public static IServiceCollection AddPlanosModule(this IServiceCollection services)
    {
        services.AddScoped<IAssinaturaPlanoRepository, AssinaturaPlanoRepository>();
        services.AddScoped<PlanoService>();
        services.AddScoped<ICotaDestaque>(sp => sp.GetRequiredService<PlanoService>());
        services.AddScoped<IPagamentoConfirmadoHandler, AssinaturaPagamentoHandler>();
        return services;
    }
}
