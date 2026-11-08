using System.Globalization;
using System.Text;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Core.Calendario;

/// <summary>
/// Gera arquivos iCalendar (.ics, RFC 5545), aceitos por Google Calendar, Apple Calendar, Outlook e outros.
/// Os horários dos eventos são de Brasília (UTC-3, sem horário de verão desde 2019) e saem convertidos para UTC.
/// </summary>
public static class IcsBuilder
{
    /// <summary>O cadastro não tem horário de término; usamos esta duração padrão.</summary>
    public static readonly TimeSpan DuracaoPadrao = TimeSpan.FromHours(2);

    private const string Crlf = "\r\n";

    public static DateTime InicioUtc(Evento e) =>
        new DateTime(e.Data.Year, e.Data.Month, e.Data.Day, e.Horario.Hour, e.Horario.Minute, 0, DateTimeKind.Utc).AddHours(3);

    public static DateTime FimUtc(Evento e) => InicioUtc(e).Add(DuracaoPadrao);

    public static string UmEvento(Evento e, string baseUrl) => Montar("Conecta Cultura", new[] { e }, baseUrl, incluirNomeCalendario: false);

    public static string Feed(IEnumerable<Evento> eventos, string baseUrl, string nomeCalendario) =>
        Montar(nomeCalendario, eventos, baseUrl, incluirNomeCalendario: true);

    private static string Montar(string nome, IEnumerable<Evento> eventos, string baseUrl, bool incluirNomeCalendario)
    {
        var linhas = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Conecta Cultura//Agenda de Eventos//PT-BR",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH"
        };

        if (incluirNomeCalendario)
        {
            linhas.Add("X-WR-CALNAME:" + Escapar(nome));
            linhas.Add("X-WR-TIMEZONE:America/Sao_Paulo");
        }

        var agora = DateTime.UtcNow;
        foreach (var e in eventos)
        {
            var url = $"{baseUrl.TrimEnd('/')}/evento.html?id={e.Id}";
            var descricao = string.IsNullOrWhiteSpace(e.Atracoes) ? url : $"{e.Atracoes}\n\nMais informações: {url}";

            linhas.Add("BEGIN:VEVENT");
            linhas.Add($"UID:evento-{e.Id}@conectacultura");
            linhas.Add("DTSTAMP:" + Formatar(agora));
            linhas.Add("DTSTART:" + Formatar(InicioUtc(e)));
            linhas.Add("DTEND:" + Formatar(FimUtc(e)));
            linhas.Add("SUMMARY:" + Escapar(e.Titulo));
            linhas.Add("LOCATION:" + Escapar($"{e.Endereco}, {e.Cidade}"));
            linhas.Add("DESCRIPTION:" + Escapar(descricao));
            linhas.Add("URL:" + url);
            linhas.Add("STATUS:CONFIRMED");
            linhas.Add("BEGIN:VALARM");
            linhas.Add("TRIGGER:-PT1H");
            linhas.Add("ACTION:DISPLAY");
            linhas.Add("DESCRIPTION:" + Escapar("Lembrete: " + e.Titulo));
            linhas.Add("END:VALARM");
            linhas.Add("END:VEVENT");
        }

        linhas.Add("END:VCALENDAR");
        return string.Concat(linhas.SelectMany(Dobrar).Select(l => l + Crlf));
    }

    public static string Formatar(DateTime utc) => utc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    /// <summary>Escape de texto do iCalendar: barra invertida, ponto e vírgula, vírgula e quebras de linha.</summary>
    public static string Escapar(string texto) =>
        texto.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,")
             .Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");

    /// <summary>Linhas do iCalendar têm no máximo 75 bytes; o excedente continua na linha seguinte iniciada por um espaço.</summary>
    internal static IEnumerable<string> Dobrar(string linha)
    {
        if (Encoding.UTF8.GetByteCount(linha) <= 75)
        {
            yield return linha;
            yield break;
        }

        var atual = new StringBuilder();
        var bytes = 0;
        var limite = 75;
        foreach (var rune in linha.EnumerateRunes())
        {
            var tamanho = rune.Utf8SequenceLength;
            if (bytes + tamanho > limite)
            {
                yield return atual.ToString();
                atual.Clear().Append(' ');
                bytes = 1;
                limite = 75;
            }

            atual.Append(rune.ToString());
            bytes += tamanho;
        }

        if (atual.Length > 0) yield return atual.ToString();
    }
}
