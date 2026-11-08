using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TechFlow.Eventos.Api.DTOs;
using TechFlow.Eventos.Core.Avaliacoes;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Models;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Api.Controllers;

/// <summary>Módulo Avaliações: nota e comentário sobre eventos que já aconteceram.</summary>
[ApiController]
[Route("api/[controller]")]
public class AvaliacoesController : ControllerBase
{
    private const string Moderadores = $"{DbSeeder.RoleAdmin},{DbSeeder.RoleModerador}";

    private readonly AvaliacaoService _service;
    private readonly IEventoRepository _eventos;

    public AvaliacoesController(AvaliacaoService service, IEventoRepository eventos)
    {
        _service = service;
        _eventos = eventos;
    }

    /// <summary>Avaliações aprovadas de um evento, com a média. Só para eventos publicados.</summary>
    [HttpGet("evento/{eventoId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> ListarDoEvento(int eventoId)
    {
        var evento = await _eventos.ObterPorIdAsync(eventoId);
        if (evento is null || evento.Status != StatusEvento.Aprovado) return NotFound();

        var resumo = await _service.ResumoAsync(eventoId);
        var itens = (await _service.ListarAsync(eventoId)).Select(a => new
        {
            a.Id, a.Nome, a.Nota, a.Comentario, a.DataEnvio
        });
        return Ok(new { resumo.Media, resumo.Total, jaAconteceu = evento.Data <= Evento.HojeBrasil(), avaliacoes = itens });
    }

    /// <summary>Resumo (média e total) de vários eventos de uma vez, para exibir nos cards. Ex.: ?ids=1,2,3</summary>
    [HttpGet("resumo")]
    [AllowAnonymous]
    public async Task<IActionResult> Resumo([FromQuery] string? ids)
    {
        var lista = (ids ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0).Distinct().Take(100).ToList();
        var resumos = await _service.ResumirAsync(lista);
        return Ok(resumos.Select(r => new { r.EventoId, r.Media, r.Total }));
    }

    /// <summary>Envia avaliação. Aberto ao público, com limite por IP. Comentário passa por moderação.</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("avaliacoes")]
    public async Task<IActionResult> Enviar(AvaliarEventoDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Website))
        {
            return Accepted(new { mensagem = "Avaliação recebida." }); // robô: finge sucesso e descarta
        }

        var r = await _service.EnviarAsync(dto.EventoId, dto.Nome, dto.Nota, dto.Comentario);
        return r.Resultado switch
        {
            ResultadoAvaliacao.Recebida => Accepted(new
            {
                publicada = r.Publicada,
                mensagem = r.Publicada ? "Obrigado pela sua avaliação!" : "Obrigado! Seu comentário será publicado depois de revisado por um moderador."
            }),
            ResultadoAvaliacao.EventoNaoEncontrado => NotFound(new { erro = "Evento não encontrado." }),
            _ => BadRequest(new { erro = r.Mensagem ?? "Avaliação inválida." })
        };
    }

    [HttpGet("pendentes")]
    [Authorize(Roles = Moderadores)]
    public async Task<IActionResult> Pendentes() =>
        Ok((await _service.ListarPendentesAsync()).Select(p => new
        {
            p.Avaliacao.Id, p.Avaliacao.EventoId, p.EventoTitulo, p.Avaliacao.Nome, p.Avaliacao.Nota,
            p.Avaliacao.Comentario, p.Avaliacao.DataEnvio
        }));

    [HttpPatch("{id:int}/moderar")]
    [Authorize(Roles = Moderadores)]
    public async Task<IActionResult> Moderar(int id, ModerarAvaliacaoDto dto) =>
        await _service.ModerarAsync(id, dto.Aprovar) ? NoContent() : NotFound();
}
