using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechFlow.Eventos.Core.Compartilhado;
using TechFlow.Eventos.Core.Pagamentos;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Api.Controllers;

/// <summary>Cobranças de pedidos e assinaturas. Modo Simulado: a pessoa "aprova" na tela. Modo Manual: um administrador confirma.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PagamentosController : ControllerBase
{
    private readonly PagamentoService _pagamentos;

    public PagamentosController(PagamentoService pagamentos) => _pagamentos = pagamentos;

    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private bool Autorizado(Pagamento p) => p.UsuarioId == UsuarioId || User.IsInRole(DbSeeder.RoleAdmin);

    [HttpGet("{referencia}")]
    public async Task<IActionResult> Obter(string referencia)
    {
        var p = await _pagamentos.ObterAsync(referencia);
        if (p is null || !Autorizado(p)) return NotFound();

        return Ok(new
        {
            p.Referencia, p.Descricao, p.Valor, status = p.Status, tipo = p.Tipo, p.Gateway,
            simulavel = _pagamentos.PermiteSimulacao && p.Status == StatusPagamento.Pendente,
            instrucoes = p.Gateway == "manual" ? _pagamentos.Instrucoes : null
        });
    }

    /// <summary>Aprova o pagamento na demonstração. Só funciona com Pagamentos:Modo = Simulado e pelo dono da cobrança.</summary>
    [HttpPost("{referencia}/simular")]
    public async Task<IActionResult> Simular(string referencia)
    {
        if (!_pagamentos.PermiteSimulacao) return NotFound();

        var p = await _pagamentos.ObterAsync(referencia);
        if (p is null || p.UsuarioId != UsuarioId) return NotFound();

        try { await _pagamentos.ConfirmarAsync(referencia); return NoContent(); }
        catch (RegraDeNegocioException ex) { return this.Responder(ex); }
    }

    /// <summary>Cobranças aguardando confirmação (para o modo Manual). Restrito a administradores.</summary>
    [HttpGet("pendentes")]
    [Authorize(Roles = DbSeeder.RoleAdmin)]
    public async Task<IActionResult> Pendentes() =>
        Ok((await _pagamentos.ListarPendentesAsync()).Select(p => new
        {
            p.Referencia, p.Descricao, p.Valor, p.UsuarioNome, p.CriadoEm, tipo = p.Tipo, p.Gateway
        }));

    /// <summary>O administrador confirma que recebeu o valor (ex.: Pix conferido no extrato).</summary>
    [HttpPost("{referencia}/confirmar")]
    [Authorize(Roles = DbSeeder.RoleAdmin)]
    public async Task<IActionResult> Confirmar(string referencia)
    {
        try { return await _pagamentos.ConfirmarAsync(referencia) ? NoContent() : NotFound(); }
        catch (RegraDeNegocioException ex) { return this.Responder(ex); }
    }
}
