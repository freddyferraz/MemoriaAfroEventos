using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using TechFlow.Eventos.Core.Geo;
using TechFlow.Eventos.Core.Interfaces;

namespace TechFlow.Eventos.Infra.Services;

/// <summary>Extração pura (sem rede) das coordenadas a partir do texto de um link do Google Maps.</summary>
public static class GoogleMapsLinkParser
{
    private const string Num = @"-?\d+(?:\.\d+)?";

    // !3d<lat>!4d<lng> é o pin exato do local; @<lat>,<lng> é só o centro do mapa (fallback).
    private static readonly Regex Pin = new($@"!3d({Num})!4d({Num})", RegexOptions.Compiled);
    private static readonly Regex Arroba = new($@"@({Num}),({Num})", RegexOptions.Compiled);
    private static readonly Regex Par = new($@"^\s*({Num})\s*,\s*({Num})\s*$", RegexOptions.Compiled);
    private static readonly string[] Parametros = { "q", "query", "destination", "ll", "center", "daddr" };

    public static Coordenadas? TentarExtrair(string link)
    {
        if (string.IsNullOrWhiteSpace(link)) return null;

        var texto = Uri.UnescapeDataString(link);
        var m = Pin.Match(texto);
        if (m.Success) return Criar(m.Groups[1].Value, m.Groups[2].Value);

        m = Arroba.Match(texto);
        if (m.Success) return Criar(m.Groups[1].Value, m.Groups[2].Value);

        if (Uri.TryCreate(link, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Query))
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            foreach (var nome in Parametros)
            {
                var par = Par.Match(query[nome] ?? string.Empty);
                if (par.Success) return Criar(par.Groups[1].Value, par.Groups[2].Value);
            }
        }

        return null;
    }

    private static Coordenadas? Criar(string lat, string lng)
    {
        if (!double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var la) ||
            !double.TryParse(lng, NumberStyles.Float, CultureInfo.InvariantCulture, out var lo))
            return null;

        return la is < -90 or > 90 || lo is < -180 or > 180 ? null : new Coordenadas(la, lo);
    }
}

public class GoogleMapsLinkService : IGeoLinkService
{
    private const int MaxRedirecionamentos = 5;

    // Só seguimos redirecionamentos dentro destes domínios, por HTTPS (evita SSRF).
    private static readonly string[] HostsPermitidos = { "maps.app.goo.gl", "goo.gl", "google.com", "google.com.br" };

    private readonly HttpClient _http;

    public GoogleMapsLinkService(HttpClient http) => _http = http;

    public async Task<Coordenadas?> ExtrairCoordenadasAsync(string link, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(link?.Trim(), UriKind.Absolute, out var atual)) return null;

        for (var i = 0; i <= MaxRedirecionamentos; i++)
        {
            var coordenadas = GoogleMapsLinkParser.TentarExtrair(atual.ToString());
            if (coordenadas is not null) return coordenadas;
            if (!HostPermitido(atual)) return null;

            using var resp = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Get, atual),
                HttpCompletionOption.ResponseHeadersRead, ct);

            if ((int)resp.StatusCode is >= 300 and < 400 && resp.Headers.Location is { } destino)
            {
                atual = destino.IsAbsoluteUri ? destino : new Uri(atual, destino);
                continue;
            }

            return null;
        }

        return null;
    }

    private static bool HostPermitido(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        var host = uri.Host.ToLowerInvariant();
        return HostsPermitidos.Any(h => host == h || host.EndsWith("." + h));
    }
}
