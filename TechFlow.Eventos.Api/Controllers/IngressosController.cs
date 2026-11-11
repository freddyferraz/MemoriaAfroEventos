using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TechFlow.Eventos.Core.Compartilhado;
using TechFlow.Eventos.Core.Ingressos;
using TechFlow.Eventos.Core.Pagamentos;
using System.ComponentModel.DataAnnotations;

namespace TechFlow.Eventos.Api.Controllers;

public class ComprarIngressoDto
{
    [Range(1, int.MaxValue)] public int EventoId { get; set; }
    [Range(1, 10)] public int Quantidade { get; set; } = 1;
}

/// <summary>Ingressos de eventos. Comprar exige conta; evento gratuito gera o ingresso na hora.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IngressosController : ControllerBase
{
    private readonly IngressoService _ingressos;
    private readonly PagamentoService _pagamentos;

    public IngressosController(IngressoService ingressos, PagamentoService pagamentos)
    {
        _ingressos = ingressos;
        _pagamentos = pagamentos;
    }

    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string UsuarioNome => User.FindFirst("nomeCompleto")?.Value ?? "Comprador";

    [HttpPost]
    [EnableRateLimiting("loja")]
    public async Task<IActionResult> Comprar(ComprarIngressoDto dto)
    {
        try
        {
            var (compra, pagamento) = await _ingressos.ComprarAsync(UsuarioId, UsuarioNome, dto.EventoId, dto.Quantidade);
            return StatusCode(StatusCodes.Status201Created, new
            {
                compraId = compra.Id, gratuito = pagamento is null, codigo = compra.Codigo,
                referencia = pagamento?.Referencia, urlPagamento = pagamento?.UrlPagamento
            });
        }
        catch (RegraDeNegocioException ex) { return this.Responder(ex); }
    }

    [HttpGet("meus")]
    public async Task<IActionResult> Meus()
    {
        var lista = new List<object>();
        foreach (var i in await _ingressos.ListarDoUsuarioAsync(UsuarioId))
        {
            string? url = null;
            if (i.Compra.Status == StatusIngresso.AguardandoPagamento)
            {
                url = (await _pagamentos.ObterPendenteDoAlvoAsync(TipoPagamento.Ingresso, i.Compra.Id))?.UrlPagamento;
            }

            lista.Add(new
            {
                i.Compra.Id, i.Compra.EventoId, evento = i.EventoTitulo, data = i.Data, horario = i.Horario, local = i.Local,
                i.Compra.Quantidade, i.Compra.PrecoUnitario, i.Compra.Total, status = i.Compra.Status, i.Compra.CriadoEm,
                codigo = i.Compra.Status == StatusIngresso.Confirmado ? i.Compra.Codigo : null,
                urlPagamento = url
            });
        }

        return Ok(lista);
    }

    [HttpPatch("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id)
    {
        try { await _ingressos.CancelarAsync(UsuarioId, id); return NoContent(); }
        catch (RegraDeNegocioException ex) { return this.Responder(ex); }
    }
}
