using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechFlow.Eventos.Api.DTOs;
using TechFlow.Eventos.Core.Compartilhado;
using TechFlow.Eventos.Core.Planos;

namespace TechFlow.Eventos.Api.Controllers;

/// <summary>Planos para organizadores. Ver os planos é público; contratar e usar a cota exigem conta.</summary>
[ApiController]
[Route("api/[controller]")]
public class PlanosController : ControllerBase
{
    private readonly PlanoService _planos;

    public PlanosController(PlanoService planos) => _planos = planos;

    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string UsuarioNome => User.FindFirst("nomeCompleto")?.Value ?? "Organizador";

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Listar() =>
        Ok(CatalogoPlanos.Todos.Select(p => new { id = (int)p.Id, p.Nome, p.DestaquesPorMes, p.PrecoMensal }));

    /// <summary>Plano em vigor da pessoa logada e a cota de destaques do mês.</summary>
    [HttpGet("meu")]
    [Authorize]
    public async Task<IActionResult> Meu()
    {
        var atual = await _planos.ObterPlanoAtualAsync(UsuarioId);
        var cota = await _planos.ObterCotaAsync(UsuarioId);
        return Ok(new
        {
            plano = new { id = (int)atual.Plano.Id, atual.Plano.Nome },
            validaAte = atual.ValidaAte,
            cota = new { cota.Limite, cota.Usado, cota.Disponivel, cota.RenovaEmUtc }
        });
    }

    /// <summary>Inicia a contratação de um plano pago (30 dias) e devolve o link do pagamento.</summary>
    [HttpPost("contratar")]
    [Authorize]
    public async Task<IActionResult> Contratar(ContratarPlanoDto dto)
    {
        try
        {
            var pagamento = await _planos.ContratarAsync(UsuarioId, UsuarioNome, (PlanoId)dto.Plano);
            return StatusCode(StatusCodes.Status201Created, new { referencia = pagamento.Referencia, urlPagamento = pagamento.UrlPagamento });
        }
        catch (RegraDeNegocioException ex) { return this.Responder(ex); }
    }
}
