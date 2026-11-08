using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechFlow.Eventos.Api.DTOs;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Recomendacoes;

namespace TechFlow.Eventos.Api.Controllers;

/// <summary>Módulo Recomendações: sugestões a partir dos interesses informados pelo navegador do visitante.</summary>
[ApiController]
[Route("api/[controller]")]
public class RecomendacoesController : ControllerBase
{
    private readonly RecomendacaoService _service;
    private readonly IPreferenciasRepository _preferencias;

    public RecomendacoesController(RecomendacaoService service, IPreferenciasRepository preferencias)
    {
        _service = service;
        _preferencias = preferencias;
    }

    private string? UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Interesses (categorias e cidades) que a pessoa logada escolheu.</summary>
    [HttpGet("meus-interesses")]
    [Authorize]
    public async Task<IActionResult> MeusInteresses()
    {
        var p = await _preferencias.ObterAsync(UsuarioId!);
        return Ok(new { categorias = p?.Categorias ?? Array.Empty<int>(), cidades = p?.Cidades ?? Array.Empty<string>() });
    }

    /// <summary>Substitui os interesses da pessoa logada.</summary>
    [HttpPut("meus-interesses")]
    [Authorize]
    public async Task<IActionResult> SalvarInteresses(InteressesDto dto)
    {
        var categorias = (dto.Categorias ?? new()).Where(c => Enum.IsDefined(typeof(CategoriaEvento), c)).Distinct().OrderBy(c => c).ToArray();
        var cidades = (dto.Cidades ?? new()).Select(c => c.Trim()).Where(c => c.Length is > 0 and <= 120)
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();

        await _preferencias.SalvarAsync(UsuarioId!, categorias, cidades);
        return NoContent();
    }

    /// <summary>
    /// Sugestões para a pessoa logada. O navegador envia só os sinais de uso (categorias com peso e cidades);
    /// o servidor soma os interesses escolhidos na conta. Nada é gravado por esta chamada.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<IEnumerable<EventoRecomendadoDto>>> Recomendar(RecomendacaoRequestDto dto)
    {
        // Sinais de uso vindos do navegador + interesses escolhidos e guardados na conta.
        var sinais = new PerfilInteresses(
            (dto.Categorias ?? new()).Select(c => new PesoCategoria(c.Categoria, c.Peso)).ToList(),
            (dto.Cidades ?? new()).Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList());
        var perfil = RecomendacaoService.Mesclar(sinais, await _preferencias.ObterAsync(UsuarioId!));

        var itens = await _service.RecomendarAsync(perfil, dto.ExcluirIds ?? new(), dto.Limite);
        return Ok(itens.Select(Mapear));
    }

    /// <summary>Eventos parecidos com um evento (mesma categoria e cidade), para "você também pode gostar".</summary>
    [HttpGet("similares/{eventoId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<EventoRecomendadoDto>>> Similares(int eventoId, [FromQuery] int limite = 4) =>
        Ok((await _service.SimilaresAsync(eventoId, limite)).Select(Mapear));

    private static EventoRecomendadoDto Mapear(EventoRecomendado r)
    {
        var e = EventoMapper.ParaDto(r.Evento);
        return new EventoRecomendadoDto
        {
            Id = e.Id, Titulo = e.Titulo, Categoria = e.Categoria, CategoriaNome = e.CategoriaNome, Cidade = e.Cidade,
            Endereco = e.Endereco, Latitude = e.Latitude, Longitude = e.Longitude, Data = e.Data, Horario = e.Horario,
            Atracoes = e.Atracoes, Preco = e.Preco, Gratuito = e.Gratuito, Acessibilidade = e.Acessibilidade,
            Ingressos = e.Ingressos, Organizador = e.Organizador, FonteOficial = e.FonteOficial, Destaque = e.Destaque,
            TemFoto = e.TemFoto, Status = e.Status, TipoAutor = e.TipoAutor, Motivo = r.Motivo
        };
    }
}
