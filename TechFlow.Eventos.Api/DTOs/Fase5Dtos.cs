using System.ComponentModel.DataAnnotations;
using TechFlow.Eventos.Core.Loja;

namespace TechFlow.Eventos.Api.DTOs;

public class AprovacaoDto
{
    public bool Aprovar { get; set; }
}

// ---------- Loja ----------
public class ProdutoCreateDto
{
    [Required, MaxLength(120)] public string Titulo { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Descricao { get; set; }
    [Required, EnumDataType(typeof(CategoriaProduto))] public CategoriaProduto Categoria { get; set; }
    [Range(0.01, 100000)] public decimal Preco { get; set; }
    [Range(0, 10000)] public int Estoque { get; set; }
    [Required, MaxLength(120)] public string Cidade { get; set; } = string.Empty;

    /// <summary>Foto opcional (JPEG, PNG ou WebP, até 2 MB).</summary>
    public IFormFile? Foto { get; set; }
}

public class ProdutoAtualizarDto
{
    [Range(0.01, 100000)] public decimal? Preco { get; set; }
    [Range(0, 10000)] public int? Estoque { get; set; }
    public bool? Ativo { get; set; }
}

public class ProdutoDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public CategoriaProduto Categoria { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public bool Disponivel { get; set; }
    public string Cidade { get; set; } = string.Empty;
    public string VendedorNome { get; set; } = string.Empty;
    public bool TemFoto { get; set; }
    public StatusProduto Status { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class PedidoCreateDto
{
    [Range(1, int.MaxValue)] public int ProdutoId { get; set; }
    [Range(1, 50)] public int Quantidade { get; set; } = 1;
    [Required, MaxLength(300)] public string EnderecoEntrega { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Telefone { get; set; } = string.Empty;
}

// ---------- Pagamentos ----------
public class PagarDto
{
    /// <summary>1 = Pix, 2 = Boleto, 3 = Cartão de crédito, 4 = Cartão de débito.</summary>
    [Range(1, 4)] public int Metodo { get; set; }
    [Range(1, 12)] public int? Parcelas { get; set; }

    /// <summary>Últimos 4 dígitos do cartão. O número completo nunca é enviado ao servidor.</summary>
    [MaxLength(4)] public string? FinalCartao { get; set; }
}

// ---------- Planos ----------
public class ContratarPlanoDto
{
    [Range(1, 4)] public int Plano { get; set; }
}
