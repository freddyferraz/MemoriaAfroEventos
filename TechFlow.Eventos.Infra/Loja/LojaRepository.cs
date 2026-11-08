using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Core.Loja;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Infra.Loja;

public class LojaRepository : ILojaRepository
{
    private readonly AppDbContext _db;

    public LojaRepository(AppDbContext db) => _db = db;

    public async Task AdicionarProdutoAsync(Produto produto) => await _db.Produtos.AddAsync(produto);

    public Task<Produto?> ObterProdutoAsync(int id) => _db.Produtos.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Produto>> BuscarAprovadosAsync(ProdutoFiltro f, int limite)
    {
        var query = _db.Produtos.AsNoTracking().Where(p => p.Status == StatusProduto.Aprovado);

        if (f.Categoria is { } c) query = query.Where(p => p.Categoria == c);
        if (!string.IsNullOrWhiteSpace(f.Cidade)) query = query.Where(p => p.Cidade == f.Cidade);
        if (f.PrecoMax is { } max) query = query.Where(p => p.Preco <= max);
        if (!string.IsNullOrWhiteSpace(f.Texto))
        {
            var t = f.Texto.Trim().ToLower();
            query = query.Where(p => p.Titulo.ToLower().Contains(t) || p.Descricao.ToLower().Contains(t) || p.VendedorNome.ToLower().Contains(t));
        }

        query = f.Ordem switch
        {
            "menor" => query.OrderBy(p => p.Preco),
            "maior" => query.OrderByDescending(p => p.Preco),
            _ => query.OrderByDescending(p => p.CriadoEm)
        };

        return await query.Take(limite).ToListAsync();
    }

    public async Task<IReadOnlyList<Produto>> ListarDoVendedorAsync(string vendedorId) =>
        await _db.Produtos.AsNoTracking().Where(p => p.VendedorId == vendedorId).OrderByDescending(p => p.CriadoEm).ToListAsync();

    public async Task<IReadOnlyList<Produto>> ListarPorStatusAsync(StatusProduto status) =>
        await _db.Produtos.AsNoTracking().Where(p => p.Status == status).OrderBy(p => p.CriadoEm).ToListAsync();

    public async Task<IReadOnlyList<string>> ListarCidadesAsync() =>
        await _db.Produtos.Where(p => p.Status == StatusProduto.Aprovado).Select(p => p.Cidade).Distinct().OrderBy(c => c).ToListAsync();

    public Task<ProdutoFoto?> ObterFotoAsync(int produtoId) =>
        _db.ProdutoFotos.AsNoTracking().FirstOrDefaultAsync(f => f.ProdutoId == produtoId);

    public async Task AdicionarPedidoAsync(Pedido pedido) => await _db.Pedidos.AddAsync(pedido);

    public Task<Pedido?> ObterPedidoAsync(int id) => _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Pedido>> ListarComprasAsync(string compradorId) =>
        await _db.Pedidos.AsNoTracking().Where(p => p.CompradorId == compradorId).OrderByDescending(p => p.CriadoEm).ToListAsync();

    public async Task<IReadOnlyList<Pedido>> ListarVendasAsync(string vendedorId) =>
        await _db.Pedidos.AsNoTracking().Where(p => p.VendedorId == vendedorId).OrderByDescending(p => p.CriadoEm).ToListAsync();

    public Task SalvarAlteracoesAsync() => _db.SaveChangesAsync();
}
