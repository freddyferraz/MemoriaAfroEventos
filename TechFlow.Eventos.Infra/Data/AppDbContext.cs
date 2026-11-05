using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Infra.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Evento> Eventos => Set<Evento>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().Property(u => u.NomeCompleto).HasMaxLength(150);

        builder.Entity<Evento>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Cidade).HasMaxLength(120).IsRequired();
            e.Property(x => x.Endereco).HasMaxLength(300).IsRequired();
            e.Property(x => x.Atracoes).HasMaxLength(2000);
            e.Property(x => x.Acessibilidade).HasMaxLength(500);
            e.Property(x => x.Ingressos).HasMaxLength(500);
            e.Property(x => x.Organizador).HasMaxLength(200).IsRequired();
            e.Property(x => x.FonteOficial).HasMaxLength(500);
            e.Property(x => x.ContatoEnvio).HasMaxLength(200).IsRequired();
            e.Property(x => x.Preco).HasPrecision(10, 2);
            e.Ignore(x => x.Gratuito);
            e.HasIndex(x => new { x.Status, x.Data });
        });
    }
}
