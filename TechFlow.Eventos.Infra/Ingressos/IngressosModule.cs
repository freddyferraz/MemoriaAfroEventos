using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechFlow.Eventos.Core.Ingressos;
using TechFlow.Eventos.Core.Models;
using TechFlow.Eventos.Core.Pagamentos;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Infra.Ingressos;

public class IngressoRepository : IIngressoRepository
{
    private readonly AppDbContext _db;

    public IngressoRepository(AppDbContext db) => _db = db;

    public async Task AdicionarAsync(CompraIngresso compra) => await _db.ComprasIngresso.AddAsync(compra);

    public Task<CompraIngresso?> ObterAsync(int id) => _db.ComprasIngresso.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IReadOnlyList<IngressoDoUsuario>> ListarDoUsuarioAsync(string usuarioId)
    {
        var linhas = await (from c in _db.ComprasIngresso.AsNoTracking()
                            join e in _db.Eventos.AsNoTracking() on c.EventoId equals e.Id
                            where c.CompradorId == usuarioId && c.Status != StatusIngresso.Cancelado
                            orderby c.CriadoEm descending
                            select new { Compra = c, e.Titulo, e.Data, e.Horario, e.Endereco, e.Cidade }).ToListAsync();
        return linhas.Select(l => new IngressoDoUsuario(l.Compra, l.Titulo, l.Data, l.Horario, $"{l.Endereco}, {l.Cidade}")).ToList();
    }

    public Task SalvarAlteracoesAsync() => _db.SaveChangesAsync();
}

public static class IngressosModelConfig
{
    public static void Aplicar(ModelBuilder b)
    {
        b.Entity<CompraIngresso>(c =>
        {
            c.Property(x => x.CompradorId).IsRequired();
            c.Property(x => x.CompradorNome).HasMaxLength(150);
            c.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
            c.HasIndex(x => x.Codigo).IsUnique();
            c.Property(x => x.PrecoUnitario).HasPrecision(12, 2);
            c.Property(x => x.Total).HasPrecision(12, 2);
            c.HasIndex(x => x.CompradorId);
            c.HasOne<Evento>().WithMany().HasForeignKey(x => x.EventoId).OnDelete(DeleteBehavior.Restrict);
            c.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CompradorId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public static class IngressosModule
{
    /// <summary>Registra o módulo Ingressos (venda de ingressos de eventos, com pagamento pelo módulo Pagamentos).</summary>
    public static IServiceCollection AddIngressosModule(this IServiceCollection services)
    {
        services.AddScoped<IIngressoRepository, IngressoRepository>();
        services.AddScoped<IngressoService>();
        services.AddScoped<IPagamentoConfirmadoHandler, IngressoPagamentoHandler>();
        return services;
    }
}
