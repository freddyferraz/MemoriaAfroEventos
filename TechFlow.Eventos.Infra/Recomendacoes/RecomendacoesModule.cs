using Microsoft.Extensions.DependencyInjection;
using TechFlow.Eventos.Core.Recomendacoes;

namespace TechFlow.Eventos.Infra.Recomendacoes;

public static class RecomendacoesModule
{
    /// <summary>Registra o módulo Recomendações (tabela de preferências das contas; os sinais de uso ficam no navegador).</summary>
    public static IServiceCollection AddRecomendacoesModule(this IServiceCollection services)
    {
        services.AddScoped<RecomendacaoService>();
        services.AddScoped<IPreferenciasRepository, PreferenciasRepository>();
        return services;
    }
}
