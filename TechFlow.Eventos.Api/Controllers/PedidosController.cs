using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Api.DTOs;
using TechFlow.Eventos.Core.Compartilhado;
using TechFlow.Eventos.Core.Loja;
using TechFlow.Eventos.Core.Pagamentos;

namespace TechFlow.Eventos.Api.Controllers;

/// <summary>Pedidos da loja. Comprar exige conta; o vendedor vê os dados de entrega só depois que o pedido é pago.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PedidosController : ControllerBase
{
    private readonly LojaService _loja;
    private readonly PagamentoService _pagamentos;

    public PedidosController(LojaService loja, PagamentoService pagamentos)
    {
        _loja = loja;
        _pagamentos = pagamentos;
    }

    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string UsuarioNome => User.FindFirst("nomeCompleto")?.Value ?? "Comprador";

    [HttpPost]
    [EnableRateLimiting("loja")]
    public async Task<IActionResult> Comprar(PedidoCreateDto dto)
    {
        try
        {
            var (pedido, pagamento) = await _loja.ComprarAsync(UsuarioId, UsuarioNome, dto.ProdutoId, dto.Quantidade, dto.EnderecoEntrega, dto.Telefone);
            return StatusCode(StatusCodes.Status201Created, new { pedidoId = pedido.Id, referencia = pagamento.Referencia, urlPagamento = pagamento.UrlPagamento });
        }
        catch (RegraDeNegocioException ex) { return this.Responder(ex); }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { erro = "O estoque mudou enquanto você comprava. Tente novamente." });
        }
    }

    [HttpGet("compras")]
    public async Task<IActionResult> Compras()
    {
        var lista = new List<object>();
        foreach (var p in await _loja.ListarComprasAsync(UsuarioId))
        {
            string? url = null;
            if (p.Status == StatusPedido.AguardandoPagamento)
            {
                url = (await _pagamentos.ObterPendenteDoAlvoAsync(TipoPagamento.Pedido, p.Id))?.UrlPagamento;
            }

            lista.Add(new
            {
                p.Id, p.ProdutoId, titulo = p.TituloProduto, p.PrecoUnitario, p.Quantidade, p.Total, status = p.Status,
                p.CriadoEm, p.EnderecoEntrega, urlPagamento = url
            });
        }

        return Ok(lista);
    }

    [HttpGet("vendas")]
    public async Task<IActionResult> Vendas() =>
        Ok((await _loja.ListarVendasAsync(UsuarioId)).Select(p =>
        {
            // Dados de entrega só depois do pagamento confirmado.
            var pago = p.Status is StatusPedido.Pago or StatusPedido.Enviado or StatusPedido.Entregue;
            return new
            {
                p.Id, p.ProdutoId, titulo = p.TituloProduto, p.PrecoUnitario, p.Quantidade, p.Total, status = p.Status, p.CriadoEm,
                compradorNome = p.CompradorNome,
                enderecoEntrega = pago ? p.EnderecoEntrega : null,
                telefone = pago ? p.Telefone : null
            };
        }));

    [HttpPatch("{id:int}/cancelar")]
    public Task<IActionResult> Cancelar(int id) => Executar(() => _loja.CancelarAsync(UsuarioId, id));

    [HttpPatch("{id:int}/enviar")]
    public Task<IActionResult> Enviar(int id) => Executar(() => _loja.MarcarEnviadoAsync(UsuarioId, id));

    [HttpPatch("{id:int}/receber")]
    public Task<IActionResult> Receber(int id) => Executar(() => _loja.ConfirmarRecebimentoAsync(UsuarioId, id));

    private async Task<IActionResult> Executar(Func<Task> acao)
    {
        try { await acao(); return NoContent(); }
        catch (RegraDeNegocioException ex) { return this.Responder(ex); }
    }
}
