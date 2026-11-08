namespace TechFlow.Eventos.Core.Loja;

public enum CategoriaProduto
{
    Artesanato = 1,
    Arte = 2,
    Alimentos = 3,
    Moda = 4,
    CasaEDecoracao = 5,
    Outros = 6
}

public enum StatusProduto
{
    Pendente = 1,
    Aprovado = 2,
    Rejeitado = 3,
    Inativo = 4
}

public enum StatusPedido
{
    AguardandoPagamento = 1,
    Pago = 2,
    Enviado = 3,
    Entregue = 4,
    Cancelado = 5
}

public static class LojaExtensions
{
    public static string Nome(this CategoriaProduto c) => c switch
    {
        CategoriaProduto.Artesanato => "Artesanato",
        CategoriaProduto.Arte => "Arte",
        CategoriaProduto.Alimentos => "Alimentos",
        CategoriaProduto.Moda => "Moda",
        CategoriaProduto.CasaEDecoracao => "Casa e decoração",
        _ => "Outros"
    };
}

public class Produto
{
    public int Id { get; set; }
    public string VendedorId { get; set; } = string.Empty;
    public string VendedorNome { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public CategoriaProduto Categoria { get; set; }
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public string Cidade { get; set; } = string.Empty;
    public StatusProduto Status { get; set; } = StatusProduto.Pendente;
    public bool TemFoto { get; set; }
    public ProdutoFoto? Foto { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

/// <summary>Foto do produto no banco, em tabela própria (as listagens não carregam os bytes).</summary>
public class ProdutoFoto
{
    public int ProdutoId { get; set; }
    public byte[] Dados { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>Pedido de um produto (um produto por pedido) com o título e o preço guardados no momento da compra.</summary>
public class Pedido
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string VendedorId { get; set; } = string.Empty;
    public string CompradorId { get; set; } = string.Empty;
    public string CompradorNome { get; set; } = string.Empty;
    public string TituloProduto { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int Quantidade { get; set; }
    public decimal Total { get; set; }
    public string EnderecoEntrega { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public StatusPedido Status { get; set; } = StatusPedido.AguardandoPagamento;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

public class ProdutoFiltro
{
    public string? Texto { get; set; }
    public CategoriaProduto? Categoria { get; set; }
    public string? Cidade { get; set; }
    public decimal? PrecoMax { get; set; }

    /// <summary>recentes (padrão), menor ou maior preço.</summary>
    public string? Ordem { get; set; }
}

public record NovoProduto(string Titulo, string Descricao, CategoriaProduto Categoria, decimal Preco, int Estoque, string Cidade);

public interface ILojaRepository
{
    Task AdicionarProdutoAsync(Produto produto);
    Task<Produto?> ObterProdutoAsync(int id);
    Task<IReadOnlyList<Produto>> BuscarAprovadosAsync(ProdutoFiltro filtro, int limite);
    Task<IReadOnlyList<Produto>> ListarDoVendedorAsync(string vendedorId);
    Task<IReadOnlyList<Produto>> ListarPorStatusAsync(StatusProduto status);
    Task<IReadOnlyList<string>> ListarCidadesAsync();
    Task<ProdutoFoto?> ObterFotoAsync(int produtoId);
    Task AdicionarPedidoAsync(Pedido pedido);
    Task<Pedido?> ObterPedidoAsync(int id);
    Task<IReadOnlyList<Pedido>> ListarComprasAsync(string compradorId);
    Task<IReadOnlyList<Pedido>> ListarVendasAsync(string vendedorId);
    Task SalvarAlteracoesAsync();
}
