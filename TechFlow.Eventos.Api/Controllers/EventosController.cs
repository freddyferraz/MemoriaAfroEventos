using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechFlow.Eventos.Api.DTOs;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Geo;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Models;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventosController : ControllerBase
{
    private const string Moderadores = $"{DbSeeder.RoleAdmin},{DbSeeder.RoleModerador}";

    private readonly IEventoRepository _repository;
    private readonly IGeoLinkService _geo;

    public EventosController(IEventoRepository repository, IGeoLinkService geo)
    {
        _repository = repository;
        _geo = geo;
    }

    /// <summary>Explorar eventos: só aprovados, de hoje em diante, com os filtros do MVP.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<EventoDto>>> Listar(
        [FromQuery] string? q, [FromQuery] string? cidade, [FromQuery] CategoriaEvento? categoria,
        [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate,
        [FromQuery] decimal? precoMax, [FromQuery] bool gratuito, [FromQuery] bool destaque,
        [FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] double? distanciaKm)
    {
        var filtro = new EventoFiltro
        {
            Texto = q, Cidade = cidade, Categoria = categoria, De = de, Ate = ate,
            PrecoMax = precoMax, SomenteGratuitos = gratuito, SomenteDestaques = destaque,
            Latitude = lat, Longitude = lng, DistanciaKm = distanciaKm
        };

        var eventos = await _repository.BuscarAprovadosAsync(filtro);
        Coordenadas? origem = lat is { } la && lng is { } lo ? new Coordenadas(la, lo) : null;
        return Ok(eventos.Select(e => ParaDto(e, origem)));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<EventoDto>> ObterPorId(int id)
    {
        var evento = await _repository.ObterPorIdAsync(id);
        return evento is null || evento.Status != StatusEvento.Aprovado ? NotFound() : Ok(ParaDto(evento));
    }

    /// <summary>Categorias disponíveis (alimenta os filtros e o formulário).</summary>
    [HttpGet("categorias")]
    [AllowAnonymous]
    public ActionResult Categorias() =>
        Ok(Enum.GetValues<CategoriaEvento>().Select(c => new { id = (int)c, nome = NomeCategoria(c) }));

    /// <summary>Cidades que têm pelo menos um evento aprovado.</summary>
    [HttpGet("cidades")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<string>>> Cidades() => Ok(await _repository.ListarCidadesAprovadasAsync());

    /// <summary>Eventos aguardando moderação, com o contato de quem enviou. Restrito a Admin/Moderador.</summary>
    [HttpGet("pendentes")]
    [Authorize(Roles = Moderadores)]
    public async Task<ActionResult<IEnumerable<EventoDto>>> ListarPendentes()
    {
        var eventos = await _repository.ListarPorStatusAsync(StatusEvento.Pendente);
        return Ok(eventos.Select(e =>
        {
            var dto = ParaDto(e);
            dto.ContatoEnvio = e.ContatoEnvio;
            return dto;
        }));
    }

    /// <summary>"Divulgar evento": aberto ao público. O evento nasce Pendente e só aparece após aprovação.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult> Enviar(EventoCreateDto dto, CancellationToken ct)
    {
        if (dto.Data < Evento.HojeBrasil())
        {
            return BadRequest(new { erro = "A data do evento não pode estar no passado." });
        }

        double? lat = dto.Latitude, lng = dto.Longitude;
        if (!string.IsNullOrWhiteSpace(dto.LinkGoogleMaps))
        {
            var coordenadas = await _geo.ExtrairCoordenadasAsync(dto.LinkGoogleMaps, ct);
            if (coordenadas is null)
            {
                return BadRequest(new { erro = "Não foi possível ler a localização do link do Google Maps. Cole o link completo do pin ou deixe o campo vazio." });
            }

            lat = coordenadas.Value.Latitude;
            lng = coordenadas.Value.Longitude;
        }

        var evento = new Evento
        {
            Titulo = dto.Titulo.Trim(), Categoria = dto.Categoria, Cidade = dto.Cidade.Trim(),
            Endereco = dto.Endereco.Trim(), Latitude = lat, Longitude = lng,
            Data = dto.Data, Horario = dto.Horario, Atracoes = dto.Atracoes, Preco = dto.Preco,
            Acessibilidade = dto.Acessibilidade, Ingressos = dto.Ingressos,
            Organizador = dto.Organizador.Trim(), FonteOficial = dto.FonteOficial,
            ContatoEnvio = dto.ContatoEnvio.Trim(), TipoAutor = dto.TipoAutor,
            Status = StatusEvento.Pendente,
            UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        await _repository.AdicionarAsync(evento);
        await _repository.SalvarAlteracoesAsync();

        return Accepted(new { id = evento.Id, mensagem = "Evento recebido. Ele será publicado depois da aprovação de um moderador." });
    }

    /// <summary>Aprova ou rejeita um evento (e define se vai para os destaques). Restrito a Admin/Moderador.</summary>
    [HttpPatch("{id:int}/moderar")]
    [Authorize(Roles = Moderadores)]
    public async Task<IActionResult> Moderar(int id, EventoModerarDto dto)
    {
        var evento = await _repository.ObterPorIdAsync(id);
        if (evento is null) return NotFound();

        evento.Status = dto.NovoStatus;
        evento.Destaque = dto.NovoStatus == StatusEvento.Aprovado && dto.Destaque;
        await _repository.SalvarAlteracoesAsync();
        return NoContent();
    }

    private static string NomeCategoria(CategoriaEvento c) => c switch
    {
        CategoriaEvento.CulturaAfro => "Cultura afro",
        CategoriaEvento.Musica => "Música",
        CategoriaEvento.Gastronomia => "Gastronomia",
        CategoriaEvento.Patrimonio => "Patrimônio",
        CategoriaEvento.Feira => "Feira",
        _ => "Outros"
    };

    private static EventoDto ParaDto(Evento e, Coordenadas? origem = null) => new()
    {
        Id = e.Id, Titulo = e.Titulo, Categoria = e.Categoria, CategoriaNome = NomeCategoria(e.Categoria),
        Cidade = e.Cidade, Endereco = e.Endereco, Latitude = e.Latitude, Longitude = e.Longitude,
        Data = e.Data, Horario = e.Horario, Atracoes = e.Atracoes, Preco = e.Preco, Gratuito = e.Gratuito,
        Acessibilidade = e.Acessibilidade, Ingressos = e.Ingressos, Organizador = e.Organizador,
        FonteOficial = e.FonteOficial, Destaque = e.Destaque, Status = e.Status, TipoAutor = e.TipoAutor,
        DistanciaKm = origem is { } o && e.Latitude is { } la && e.Longitude is { } lo
            ? Math.Round(o.DistanciaKm(new Coordenadas(la, lo)), 1)
            : null
    };
}
