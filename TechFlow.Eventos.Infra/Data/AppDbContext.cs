using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Core.Avaliacoes;
using TechFlow.Eventos.Core.Models;
using TechFlow.Eventos.Core.Recomendacoes;
using TechFlow.Eventos.Core.Newsletter;
using TechFlow.Eventos.Infra.Avaliacoes;
using TechFlow.Eventos.Infra.Newsletter;
using TechFlow.Eventos.Infra.Recomendacoes;

namespace TechFlow.Eventos.Infra.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<EventoFoto> Fotos => Set<EventoFoto>();
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();
    public DbSet<PreferenciasUsuario> Preferencias => Set<PreferenciasUsuario>();
    public DbSet<Assinante> Assinantes => Set<Assinante>();
    public DbSet<EnvioNewsletter> EnviosNewsletter => Set<EnvioNewsletter>();

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
            e.HasOne(x => x.Foto).WithOne(f => f.Evento).HasForeignKey<EventoFoto>(f => f.EventoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EventoFoto>(f =>
        {
            f.HasKey(x => x.EventoId);
            f.Property(x => x.Dados).IsRequired();
            f.Property(x => x.ContentType).HasMaxLength(50).IsRequired();
        });

        NewsletterModelConfig.Aplicar(builder);
        AvaliacoesModelConfig.Aplicar(builder);
        RecomendacoesModelConfig.Aplicar(builder);
    }
}
