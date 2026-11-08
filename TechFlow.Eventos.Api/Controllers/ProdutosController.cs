using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TechFlow.Eventos.Api.DTOs;
using TechFlow.Eventos.Core.Compartilhado;
using TechFlow.Eventos.Core.Imagens;
using TechFlow.Eventos.Core.Loja;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Api.Controllers;

/// <summary>Loja: qualquer pessoa vê os produtos; para anunciar é preciso estar logada.</summary>
[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private const string Moderadores = $"{DbSeeder.RoleAdmin},{DbSeeder.RoleModerador}";

    private readonly LojaService _loja;

    public ProdutosController(LojaService loja) => _loja = loja;

    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string UsuarioNome => User.FindFirst("nomeCompleto")?.Value ?? "Vendedor";

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar([FromQuery] string? q, [FromQuery] CategoriaProduto? categoria, [FromQuery] string? cidade,
        [FromQuery] decimal? precoMax, [FromQuery] string? ordem)
    {
        var itens = await _loja.BuscarAsync(new ProdutoFiltro { Texto = q, Categoria = categoria, Cidade = cidade, PrecoMax = precoMax, Ordem = ordem });
        return Ok(itens.Select(Mapear));
    }

    [HttpGet("categorias")]
    [AllowAnonymous]
    public IActionResult Categorias() =>
        Ok(Enum.GetValues<CategoriaProduto>().Select(c => new { id = (int)c, nome = c.Nome() }));

    [HttpGet("cidades")]
    [AllowAnonymous]
    public async Task<IActionResult> Cidades() => Ok(await _loja.ListarCidadesAsync());

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Obter(int id)
    {
        var produto = await _loja.ObterProdutoAsync(id);
        return produto is null || produto.Status != StatusProduto.Aprovado ? NotFound() : Ok(Mapear(produto));
    }

    /// <summary>Foto pública para produtos aprovados; produtos pendentes ou pausados só para o dono e moderadores.</summary>
    [HttpGet("{id:int}/foto")]
    [AllowAnonymous]
    public async Task<IActionResult> Foto(int id)
    {
        var produto = await _loja.ObterProdutoAsync(id);
        if (produto is null || !produto.TemFoto) return NotFound();

        var publico = produto.Status == StatusProduto.Aprovado;
        var autorizado = publico || produto.VendedorId == UsuarioId
            || User.IsInRole(DbSeeder.RoleAdmin) || User.IsInRole(DbSeeder.RoleModerador);
        if (!autorizado) return NotFound();

        var foto = await _loja.ObterFotoAsync(id);
        if (foto is null) return NotFound();

        Response.Headers.CacheControl = publico ? "public, max-age=86400" : "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(foto.Dados, foto.ContentType);
    }

    /// <summary>Meus anúncios, em qualquer situação (pendente, aprovado, pausado, rejeitado).</summary>
    [HttpGet("meus")]
    [Authorize]
    public async Task<IActionResult> Meus() => Ok((await _loja.ListarDoVendedorAsync(UsuarioId)).Select(Mapear));

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("loja")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> Anunciar([FromForm] ProdutoCreateDto dto, CancellationToken ct)
    {
        byte[]? foto = null;
        if (dto.Foto is { Length: > 0 })
        {
            if (dto.Foto.Length > TipoImagem.TamanhoMaximoBytes) return BadRequest(new { erro = "A foto deve ter no máximo 2 MB." });
            using var ms = new MemoryStream();
            await dto.Foto.CopyToAsync(ms, ct);
            foto = ms.ToArray();
        }

        try
        {
            var p = await _loja.CriarProdutoAsync(UsuarioId, UsuarioNome,
                new NovoProduto(dto.Titulo, dto.Descricao ?? string.Empty, dto.Categoria, dto.Preco, dto.Estoque, dto.Cidade), foto);
            return Accepted(new { id = p.Id, mensagem = "Anúncio recebido. Ele será publicado depois da aprovação de um moderador." });
        }
        catch (RegraDeNegocioException ex) { return this.Responder(ex); }
    }

    /// <summary>O vendedor ajusta preço e estoque, ou pausa e reativa o anúncio.</summary>
    [HttpPatch("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Atualizar(int id, ProdutoAtualizarDto dto)
    {
        try { return Ok(Mapear(await _loja.AtualizarProdutoAsync(UsuarioId, id, dto.Preco, dto.Estoque, dto.Ativo))); }
        catch (RegraDeNegocioException ex) { return this.Responder(ex); }
    }

    [HttpGet("pendentes")]
    [Authorize(Roles = Moderadores)]
    public async Task<IActionResult> Pendentes() => Ok((await _loja.ListarPendentesAsync()).Select(Mapear));

    [HttpPatch("{id:int}/moderar")]
    [Authorize(Roles = Moderadores)]
    public async Task<IActionResult> Moderar(int id, AprovacaoDto dto)
    {
        try { await _loja.ModerarProdutoAsync(id, dto.Aprovar); return NoContent(); }
        catch (RegraDeNegocioException ex) { return this.Responder(ex); }
    }

    private static ProdutoDto Mapear(Produto p) => new()
    {
        Id = p.Id, Titulo = p.Titulo, Descricao = p.Descricao, Categoria = p.Categoria, CategoriaNome = p.Categoria.Nome(),
        Preco = p.Preco, Estoque = p.Estoque, Disponivel = p.Estoque > 0, Cidade = p.Cidade, VendedorNome = p.VendedorNome,
        TemFoto = p.TemFoto, Status = p.Status, CriadoEm = p.CriadoEm
    };
}
