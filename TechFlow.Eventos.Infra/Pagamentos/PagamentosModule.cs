using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechFlow.Eventos.Core.Pagamentos;

namespace TechFlow.Eventos.Infra.Pagamentos;

public static class PagamentosModelConfig
{
    public static void Aplicar(ModelBuilder b)
    {
        b.Entity<Pagamento>(p =>
        {
            p.Property(x => x.Referencia).HasMaxLength(64).IsRequired();
            p.HasIndex(x => x.Referencia).IsUnique();
            p.Property(x => x.Valor).HasPrecision(12, 2);
            p.Property(x => x.Descricao).HasMaxLength(300);
            p.Property(x => x.UsuarioNome).HasMaxLength(150);
            p.Property(x => x.Gateway).HasMaxLength(30);
            p.Property(x => x.FinalCartao).HasMaxLength(4);
            p.Property(x => x.CodigoPix).HasMaxLength(400);
            p.Property(x => x.CodigoBoleto).HasMaxLength(60);
            p.Ignore(x => x.UrlPagamento);
            p.HasIndex(x => new { x.Tipo, x.AlvoId });
        });
    }
}

/// <summary>Demonstração: a própria pessoa confirma o pagamento na tela. Nenhum valor real é cobrado.</summary>
public sealed class GatewaySimulado : IPagamentoGateway
{
    public string Nome => "simulado";
    public bool PermiteSimulacao => true;
    public string? Instrucoes => null;
}

/// <summary>Pagamento feito por fora (por exemplo, Pix para a chave da plataforma); um administrador confirma o recebimento.</summary>
public sealed class GatewayManual : IPagamentoGateway
{
    public GatewayManual(string? instrucoes) => Instrucoes = instrucoes;

    public string Nome => "manual";
    public bool PermiteSimulacao => false;
    public string? Instrucoes { get; }
}

public static class PagamentosModule
{
    /// <summary>
    /// Registra o módulo Pagamentos. Pagamentos:Modo define o que acontece:
    /// Desativado (padrão: ninguém consegue comprar nem assinar), Simulado (demonstração) ou Manual (confirmação por um administrador).
    /// Pagamentos:Instrucoes é o texto exibido no modo Manual (ex.: chave Pix e como enviar o comprovante).
    /// </summary>
    public static IServiceCollection AddPagamentosModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IPagamentoRepository, PagamentoRepository>();
        services.AddScoped<PagamentoService>();

        switch ((config["Pagamentos:Modo"] ?? "Desativado").Trim().ToLowerInvariant())
        {
            case "simulado":
                services.AddSingleton<IPagamentoGateway, GatewaySimulado>();
                break;
            case "manual":
                services.AddSingleton<IPagamentoGateway>(new GatewayManual(config["Pagamentos:Instrucoes"]));
                break;
        }

        return services;
    }
}
