using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechFlow.Eventos.Core.Newsletter;

namespace TechFlow.Eventos.Infra.Newsletter;

/// <summary>
/// Envio pela API HTTP do Brevo (https://developers.brevo.com). Usa HTTPS (porta 443), então funciona
/// no Render, que bloqueia SMTP (25, 465 e 587) nos serviços gratuitos. O remetente precisa estar verificado no Brevo.
/// </summary>
public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly NewsletterOptions _options;
    private readonly string _apiKey;

    public BrevoEmailSender(HttpClient http, NewsletterOptions options, IConfiguration configuration)
    {
        _http = http;
        _options = options;
        _apiKey = configuration["Email:Brevo:ApiKey"] ?? string.Empty;
    }

    public async Task EnviarAsync(EmailMensagem m, CancellationToken ct = default)
    {
        var payload = new
        {
            sender = new { name = _options.NomeRemetente, email = _options.EmailRemetente },
            to = new[] { new { email = m.Para } },
            subject = m.Assunto,
            htmlContent = m.Html,
            textContent = m.Texto
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Add("api-key", _apiKey);

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var corpo = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Brevo respondeu {(int)resp.StatusCode}: {(corpo.Length > 300 ? corpo[..300] : corpo)}");
        }
    }
}

/// <summary>Sem provedor configurado, o e-mail é apenas registrado no log (útil em desenvolvimento: os links aparecem no console).</summary>
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _log;

    public LogEmailSender(ILogger<LogEmailSender> log) => _log = log;

    public Task EnviarAsync(EmailMensagem m, CancellationToken ct = default)
    {
        _log.LogWarning("E-mail NÃO enviado (provedor não configurado). Para: {Para} | Assunto: {Assunto}\n{Texto}", m.Para, m.Assunto, m.Texto);
        return Task.CompletedTask;
    }
}
