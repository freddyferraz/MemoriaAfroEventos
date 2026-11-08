using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechFlow.Eventos.Core.Calendario;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Api.Controllers;

/// <summary>Módulo Calendário: arquivo .ics, links para calendários na web e feed assinável.</summary>
[ApiController]
[Route("api/[controller]")]
public class CalendarioController : ControllerBase
{
    private readonly IEventoRepository _eventos;

    public CalendarioController(IEventoRepository eventos) => _eventos = eventos;

    private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

    /// <summary>Baixa o .ics de um evento (abre no Apple Calendar, Outlook, Google Agenda e outros).</summary>
    [HttpGet("evento/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Ics(int id)
    {
        var evento = await _eventos.ObterPorIdAsync(id);
        if (evento is null || evento.Status != StatusEvento.Aprovado) return NotFound();

        var bytes = new UTF8Encoding(false).GetBytes(IcsBuilder.UmEvento(evento, BaseUrl));
        return File(bytes, "text/calendar; charset=utf-8", $"evento-{id}.ics");
    }

    /// <summary>Links "adicionar ao calendário" (Google, Outlook e Microsoft 365) e o caminho do .ics.</summary>
    [HttpGet("evento/{id:int}/links")]
    [AllowAnonymous]
    public async Task<IActionResult> Links(int id)
    {
        var evento = await _eventos.ObterPorIdAsync(id);
        if (evento is null || evento.Status != StatusEvento.Aprovado) return NotFound();

        var url = $"{BaseUrl}/evento.html?id={id}";
        return Ok(new
        {
            google = CalendarioLinks.Google(evento, url),
            outlook = CalendarioLinks.OutlookPessoal(evento, url),
            microsoft365 = CalendarioLinks.Microsoft365(evento, url),
            ics = $"/api/calendario/evento/{id}"
        });
    }

    /// <summary>
    /// Feed iCalendar com os próximos eventos aprovados (filtro opcional por categoria e cidade).
    /// Pode ser assinado no Google Agenda, Apple Calendar ou Outlook, que o atualizam sozinhos.
    /// </summary>
    [HttpGet("feed.ics")]
    [AllowAnonymous]
    public async Task<IActionResult> Feed([FromQuery] CategoriaEvento? categoria, [FromQuery] string? cidade)
    {
        var eventos = await _eventos.BuscarAprovadosAsync(new EventoFiltro { Categoria = categoria, Cidade = cidade });
        var nome = "Conecta Cultura" + (categoria is { } c ? $" - {c.Nome()}" : string.Empty);

        Response.Headers.CacheControl = "public, max-age=900";
        return Content(IcsBuilder.Feed(eventos.Take(300), BaseUrl, nome), "text/calendar", new UTF8Encoding(false));
    }
}
