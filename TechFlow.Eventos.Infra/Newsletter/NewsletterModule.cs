using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Newsletter;

namespace TechFlow.Eventos.Infra.Newsletter;

public static class NewsletterModule
{
    /// <summary>
    /// Registra o módulo Newsletter. Configuração: App:BaseUrl, Email:RemetenteNome, Email:RemetenteEmail e Email:Brevo:ApiKey.
    /// Sem ApiKey e remetente, usa o LogEmailSender (nada é enviado de verdade).
    /// </summary>
    public static IServiceCollection AddNewsletterModule(this IServiceCollection services, IConfiguration config)
    {
        var options = new NewsletterOptions
        {
            BaseUrl = config["App:BaseUrl"] ?? string.Empty,
            NomeRemetente = config["Email:RemetenteNome"] ?? "TechFlow Eventos",
            EmailRemetente = config["Email:RemetenteEmail"] ?? string.Empty
        };
        services.AddSingleton(options);

        services.AddScoped<IAssinanteRepository, AssinanteRepository>();
        services.AddScoped<NewsletterService>();

        services.AddSingleton<NewsletterQueue>();
        services.AddSingleton<INewsletterDispatcher>(sp => sp.GetRequiredService<NewsletterQueue>());
        services.AddHostedService<NewsletterWorker>();
        services.AddScoped<IEventoPublicadoHandler, NewsletterEventoPublicadoHandler>();

        if (!string.IsNullOrWhiteSpace(config["Email:Brevo:ApiKey"]) && !string.IsNullOrWhiteSpace(options.EmailRemetente))
        {
            services.AddHttpClient<IEmailSender, BrevoEmailSender>(c => c.Timeout = TimeSpan.FromSeconds(15));
        }
        else
        {
            services.AddSingleton<IEmailSender, LogEmailSender>();
        }

        return services;
    }
}
