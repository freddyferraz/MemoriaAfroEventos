using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechFlow.Eventos.Api.DTOs;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Geo;
using TechFlow.Eventos.Core.Imagens;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Models;
using TechFlow.Eventos.Core.Planos;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventosController : ControllerBase
{
    private const string Moderadores = $"{DbSeeder.RoleAdmin},{DbSeeder.RoleModerador}";

    private readonly IEventoRepository _repository;
    private readonly IGeoLinkService _geo;
    private readonly IEnumerable<IEventoPublicadoHandler> _aoPublicar;
    private readonly ICotaDestaque? _cotas;

    // ICotaDestaque é opcional: sem o módulo de Planos, não existe pedido de destaque.
    public EventosController(IEventoRepository repository, IGeoLinkService geo, IEnumerable<IEventoPublicadoHandler> aoPublicar, ICotaDestaque? cotas = null)
    {
        _cotas = cotas;
        _repository = repository;
        _geo = geo;
        _aoPublicar = aoPublicar;
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

    /// <summary>
    /// Foto do evento. Pública para eventos aprovados; para pendentes/rejeitados só moderadores
    /// (a tela de moderação busca a imagem com o token).
    /// </summary>
    [HttpGet("{id:int}/foto")]
    [AllowAnonymous]
    public async Task<IActionResult> ObterFoto(int id)
    {
        var evento = await _repository.ObterPorIdAsync(id);
        if (evento is null || !evento.TemFoto) return NotFound();

        var publico = evento.Status == StatusEvento.Aprovado;
        if (!publico && !User.IsInRole(DbSeeder.RoleAdmin) && !User.IsInRole(DbSeeder.RoleModerador))
        {
            return NotFound();
        }

        var foto = await _repository.ObterFotoAsync(id);
        if (foto is null) return NotFound();

        Response.Headers.CacheControl = publico ? "public, max-age=86400" : "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(foto.Dados, foto.ContentType);
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
            dto.DestaqueSolicitado = e.DestaqueSolicitado;
            return dto;
        }));
    }

    /// <summary>"Divulgar evento": aberto ao público. O evento nasce Pendente e só aparece após aprovação.</summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<ActionResult> Enviar([FromForm] EventoCreateDto dto, CancellationToken ct)
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

        // Destaque do plano: exige conta e cota disponível no mês.
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (dto.PedirDestaque)
        {
            if (usuarioId is null) return BadRequest(new { erro = "Entre na sua conta para usar um destaque do seu plano." });
            if (_cotas is null) return BadRequest(new { erro = "Os planos não estão disponíveis." });

            var cota = await _cotas.ObterCotaAsync(usuarioId);
            if (cota.Disponivel <= 0)
            {
                return BadRequest(new { erro = $"Você já usou os {cota.Limite} destaque(s) do plano {cota.Plano} neste mês. Veja os planos para ter mais." });
            }
        }

        EventoFoto? foto = null;
        if (dto.Foto is { Length: > 0 })
        {
            if (dto.Foto.Length > TipoImagem.TamanhoMaximoBytes)
            {
                return BadRequest(new { erro = "A foto deve ter no máximo 2 MB." });
            }

            using var ms = new MemoryStream();
            await dto.Foto.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();
            var tipo = TipoImagem.Detectar(bytes);
            if (tipo is null)
            {
                return BadRequest(new { erro = "Formato de foto não aceito. Use JPEG, PNG ou WebP." });
            }

            foto = new EventoFoto { Dados = bytes, ContentType = tipo };
        }

        var evento = new Evento
        {
            Titulo = dto.Titulo.Trim(), Categoria = dto.Categoria, Cidade = dto.Cidade.Trim(),
            Endereco = dto.Endereco.Trim(), Latitude = lat, Longitude = lng,
            Data = dto.Data, Horario = dto.Horario, Atracoes = dto.Atracoes?.Trim() ?? string.Empty, Preco = dto.Preco,
            Acessibilidade = dto.Acessibilidade?.Trim() ?? string.Empty, Ingressos = dto.Ingressos?.Trim() ?? string.Empty,
            Organizador = dto.Organizador.Trim(), FonteOficial = dto.FonteOficial?.Trim() ?? string.Empty,
            TemFoto = foto is not null, Foto = foto,
            ContatoEnvio = dto.ContatoEnvio.Trim(), TipoAutor = dto.TipoAutor,
            Status = StatusEvento.Pendente,
            UsuarioId = usuarioId,
            DestaqueSolicitado = dto.PedirDestaque && usuarioId is not null
        };

        await _repository.AdicionarAsync(evento);
        await _repository.SalvarAlteracoesAsync();

        return Accepted(new { id = evento.Id, mensagem = "Evento recebido. Ele será publicado depois da aprovação de um moderador." });
    }

    /// <summary>Aprova ou rejeita um evento (e define se vai para os destaques). Restrito a Admin/Moderador.</summary>
    [HttpPatch("{id:int}/moderar")]
    [Authorize(Roles = Moderadores)]
    public async Task<IActionResult> Moderar(int id, EventoModerarDto dto, CancellationToken ct)
    {
        var evento = await _repository.ObterPorIdAsync(id);
        if (evento is null) return NotFound();

        var jaEstavaAprovado = evento.Status == StatusEvento.Aprovado;
        evento.Status = dto.NovoStatus;
        // Quem usou a cota do plano tem o destaque garantido na aprovação.
        evento.Destaque = dto.NovoStatus == StatusEvento.Aprovado && (dto.Destaque || evento.DestaqueSolicitado);
        await _repository.SalvarAlteracoesAsync();

        // Outros módulos (ex.: newsletter) reagem à publicação sem que este controller os conheça.
        if (!jaEstavaAprovado && evento.Status == StatusEvento.Aprovado)
        {
            foreach (var handler in _aoPublicar) await handler.AoPublicarAsync(evento, ct);
        }

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
        FonteOficial = e.FonteOficial, Destaque = e.Destaque, TemFoto = e.TemFoto, Status = e.Status, TipoAutor = e.TipoAutor,
        DistanciaKm = origem is { } o && e.Latitude is { } la && e.Longitude is { } lo
            ? Math.Round(o.DistanciaKm(new Coordenadas(la, lo)), 1)
            : null
    };
}
