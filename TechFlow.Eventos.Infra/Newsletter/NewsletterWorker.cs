using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Models;
using TechFlow.Eventos.Core.Newsletter;

namespace TechFlow.Eventos.Infra.Newsletter;

/// <summary>Fila em memória de eventos a notificar. Se a aplicação reiniciar com itens na fila, eles se perdem (limitação do MVP).</summary>
public sealed class NewsletterQueue : INewsletterDispatcher
{
    private readonly Channel<int> _canal = Channel.CreateUnbounded<int>();

    public void EnfileirarEvento(int eventoId) => _canal.Writer.TryWrite(eventoId);

    public IAsyncEnumerable<int> LerAsync(CancellationToken ct) => _canal.Reader.ReadAllAsync(ct);
}

/// <summary>Processa a fila em segundo plano, fora da requisição de moderação.</summary>
public class NewsletterWorker : BackgroundService
{
    private readonly NewsletterQueue _fila;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<NewsletterWorker> _log;

    public NewsletterWorker(NewsletterQueue fila, IServiceScopeFactory scopes, ILogger<NewsletterWorker> log)
    {
        _fila = fila;
        _scopes = scopes;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var eventoId in _fila.LerAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var servico = scope.ServiceProvider.GetRequiredService<NewsletterService>();
                    var r = await servico.NotificarEventoAsync(eventoId, stoppingToken);
                    _log.LogInformation("Newsletter do evento {Id}: {Enviados}/{Destinatarios} enviados, {Falhas} falhas. {Erro}",
                        eventoId, r.Enviados, r.Destinatarios, r.Falhas, r.UltimoErro);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogError(ex, "Falha ao processar a newsletter do evento {Id}.", eventoId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // desligamento da aplicação
        }
    }
}

/// <summary>Liga o módulo de Eventos ao de Newsletter: ao aprovar um evento, ele entra na fila.</summary>
public class NewsletterEventoPublicadoHandler : IEventoPublicadoHandler
{
    private readonly INewsletterDispatcher _dispatcher;

    public NewsletterEventoPublicadoHandler(INewsletterDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task AoPublicarAsync(Evento evento, CancellationToken ct = default)
    {
        _dispatcher.EnfileirarEvento(evento.Id);
        return Task.CompletedTask;
    }
}
