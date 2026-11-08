using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Core.Avaliacoes;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Infra.Avaliacoes;

public static class AvaliacoesModelConfig
{
    public static void Aplicar(ModelBuilder b)
    {
        b.Entity<Avaliacao>(a =>
        {
            a.Property(x => x.Nome).HasMaxLength(60).IsRequired();
            a.Property(x => x.Comentario).HasMaxLength(1000);
            a.HasOne<Evento>().WithMany().HasForeignKey(x => x.EventoId).OnDelete(DeleteBehavior.Cascade);
            a.HasIndex(x => new { x.EventoId, x.Status });
        });
    }
}
