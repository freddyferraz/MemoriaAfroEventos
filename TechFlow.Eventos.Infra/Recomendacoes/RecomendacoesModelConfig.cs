using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Core.Recomendacoes;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Infra.Recomendacoes;

public static class RecomendacoesModelConfig
{
    public static void Aplicar(ModelBuilder b)
    {
        b.Entity<PreferenciasUsuario>(p =>
        {
            p.HasKey(x => x.UsuarioId);
            p.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
