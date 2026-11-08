using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechFlow.Eventos.Core.Loja;
using TechFlow.Eventos.Core.Pagamentos;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Infra.Loja;

public static class LojaModelConfig
{
    public static void Aplicar(ModelBuilder b)
    {
        b.Entity<Produto>(p =>
        {
            p.Property(x => x.VendedorId).IsRequired();
            p.Property(x => x.VendedorNome).HasMaxLength(150);
            p.Property(x => x.Titulo).HasMaxLength(120).IsRequired();
            p.Property(x => x.Descricao).HasMaxLength(2000);
            p.Property(x => x.Cidade).HasMaxLength(120).IsRequired();
            p.Property(x => x.Preco).HasPrecision(12, 2);
            p.HasIndex(x => new { x.Status, x.Categoria });
            p.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.VendedorId).OnDelete(DeleteBehavior.Cascade);
            p.HasOne(x => x.Foto).WithOne().HasForeignKey<ProdutoFoto>(f => f.ProdutoId).OnDelete(DeleteBehavior.Cascade);

            // Controle de concorrência do Postgres (coluna de sistema xmin): impede que duas compras simultâneas levem o mesmo estoque.
            p.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        });

        b.Entity<ProdutoFoto>(f =>
        {
            f.HasKey(x => x.ProdutoId);
            f.Property(x => x.Dados).IsRequired();
            f.Property(x => x.ContentType).HasMaxLength(50).IsRequired();
        });

        b.Entity<Pedido>(p =>
        {
            p.Property(x => x.TituloProduto).HasMaxLength(120);
            p.Property(x => x.CompradorNome).HasMaxLength(150);
            p.Property(x => x.EnderecoEntrega).HasMaxLength(300);
            p.Property(x => x.Telefone).HasMaxLength(30);
            p.Property(x => x.PrecoUnitario).HasPrecision(12, 2);
            p.Property(x => x.Total).HasPrecision(12, 2);
            p.HasIndex(x => x.CompradorId);
            p.HasIndex(x => x.VendedorId);
            p.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public static class LojaModule
{
    public static IServiceCollection AddLojaModule(this IServiceCollection services)
    {
        services.AddScoped<ILojaRepository, LojaRepository>();
        services.AddScoped<LojaService>();
        services.AddScoped<IPagamentoConfirmadoHandler, PedidoPagamentoHandler>();
        return services;
    }
}
