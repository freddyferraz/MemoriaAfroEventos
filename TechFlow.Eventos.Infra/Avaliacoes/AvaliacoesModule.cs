using Microsoft.Extensions.DependencyInjection;
using TechFlow.Eventos.Core.Avaliacoes;

namespace TechFlow.Eventos.Infra.Avaliacoes;

public static class AvaliacoesModule
{
    /// <summary>Registra o módulo Avaliações. Para removê-lo, apague esta chamada e a rota/controller correspondente.</summary>
    public static IServiceCollection AddAvaliacoesModule(this IServiceCollection services)
    {
        services.AddScoped<IAvaliacaoRepository, AvaliacaoRepository>();
        services.AddScoped<AvaliacaoService>();
        return services;
    }
}
