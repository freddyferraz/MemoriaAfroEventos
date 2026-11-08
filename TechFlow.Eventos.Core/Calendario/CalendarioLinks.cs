using System.Globalization;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Core.Calendario;

/// <summary>Links "adicionar à agenda" para os calendários na web (o .ics cobre Apple e demais aplicativos).</summary>
public static class CalendarioLinks
{
    public static string Google(Evento e, string urlEvento)
    {
        var datas = $"{IcsBuilder.Formatar(IcsBuilder.InicioUtc(e))}/{IcsBuilder.Formatar(IcsBuilder.FimUtc(e))}";
        return "https://calendar.google.com/calendar/render?action=TEMPLATE"
            + $"&text={Uri.EscapeDataString(e.Titulo)}"
            + $"&dates={datas}"
            + $"&details={Uri.EscapeDataString(Detalhes(e, urlEvento))}"
            + $"&location={Uri.EscapeDataString(Local(e))}";
    }

    public static string OutlookPessoal(Evento e, string urlEvento) => Outlook("https://outlook.live.com", e, urlEvento);

    public static string Microsoft365(Evento e, string urlEvento) => Outlook("https://outlook.office.com", e, urlEvento);

    private static string Outlook(string host, Evento e, string urlEvento) =>
        $"{host}/calendar/0/deeplink/compose?path=%2Fcalendar%2Faction%2Fcompose&rru=addevent"
        + $"&subject={Uri.EscapeDataString(e.Titulo)}"
        + $"&startdt={Uri.EscapeDataString(IcsBuilder.InicioUtc(e).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture))}"
        + $"&enddt={Uri.EscapeDataString(IcsBuilder.FimUtc(e).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture))}"
        + $"&body={Uri.EscapeDataString(Detalhes(e, urlEvento))}"
        + $"&location={Uri.EscapeDataString(Local(e))}";

    private static string Local(Evento e) => $"{e.Endereco}, {e.Cidade}";

    private static string Detalhes(Evento e, string urlEvento) =>
        string.IsNullOrWhiteSpace(e.Atracoes) ? urlEvento : $"{e.Atracoes}\n\nMais informações: {urlEvento}";
}
