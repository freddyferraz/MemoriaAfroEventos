using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Core.Models;
using TechFlow.Eventos.Core.Newsletter;

namespace TechFlow.Eventos.Infra.Newsletter;

/// <summary>Mapeamento das tabelas do módulo, chamado de uma linha no AppDbContext.</summary>
public static class NewsletterModelConfig
{
    public static void Aplicar(ModelBuilder b)
    {
        b.Entity<Assinante>(a =>
        {
            a.Property(x => x.Email).HasMaxLength(254).IsRequired();
            a.HasIndex(x => x.Email).IsUnique();
            a.Property(x => x.Token).HasMaxLength(64).IsRequired();
            a.HasIndex(x => x.Token).IsUnique();
        });

        b.Entity<EnvioNewsletter>(e =>
        {
            e.HasOne<Assinante>().WithMany().HasForeignKey(x => x.AssinanteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Evento>().WithMany().HasForeignKey(x => x.EventoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.EventoId, x.AssinanteId }).IsUnique();
        });
    }
}
