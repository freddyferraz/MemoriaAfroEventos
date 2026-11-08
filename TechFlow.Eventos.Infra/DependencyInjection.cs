using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Infra.Data;
using TechFlow.Eventos.Infra.Repositories;
using TechFlow.Eventos.Infra.Services;

namespace TechFlow.Eventos.Infra;

public static class DependencyInjection
{
    /// <summary>Registra DbContext (Postgres), Identity, repositórios e serviços da infraestrutura.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddErrorDescriber<IdentityErrorDescriberPt>()
            .AddDefaultTokenProviders();

        services.AddHttpClient<IGeoLinkService, GoogleMapsLinkService>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(10);
                c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; TechFlowEventos/1.0)");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        services.AddScoped<IEventoRepository, EventoRepository>();
        services.AddScoped<ITokenService, TokenService>();
        return services;
    }
}
