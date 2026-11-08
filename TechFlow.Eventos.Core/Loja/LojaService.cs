using TechFlow.Eventos.Core.Compartilhado;
using TechFlow.Eventos.Core.Imagens;
using TechFlow.Eventos.Core.Pagamentos;

namespace TechFlow.Eventos.Core.Loja;

/// <summary>
/// Regras da loja: produtos de artistas, artesãos, produtores e empreendedores locais.
/// Ver produtos é público; anunciar e comprar exigem conta. Produto novo passa pela moderação.
/// </summary>
public class LojaService
{
    private readonly ILojaRepository _repositorio;
    private readonly PagamentoService _pagamentos;

    public LojaService(ILojaRepository repositorio, PagamentoService pagamentos)
    {
        _repositorio = repositorio;
        _pagamentos = pagamentos;
    }

    // ---------- Leitura ----------
    public Task<IReadOnlyList<Produto>> BuscarAsync(ProdutoFiltro filtro) => _repositorio.BuscarAprovadosAsync(filtro, 60);
    public Task<Produto?> ObterProdutoAsync(int id) => _repositorio.ObterProdutoAsync(id);
    public Task<IReadOnlyList<Produto>> ListarDoVendedorAsync(string vendedorId) => _repositorio.ListarDoVendedorAsync(vendedorId);
    public Task<IReadOnlyList<Produto>> ListarPendentesAsync() => _repositorio.ListarPorStatusAsync(StatusProduto.Pendente);
    public Task<IReadOnlyList<string>> ListarCidadesAsync() => _repositorio.ListarCidadesAsync();
    public Task<ProdutoFoto?> ObterFotoAsync(int produtoId) => _repositorio.ObterFotoAsync(produtoId);

    // ---------- Anunciar ----------
    public async Task<Produto> CriarProdutoAsync(string vendedorId, string vendedorNome, NovoProduto d, byte[]? foto)
    {
        var titulo = d.Titulo.Trim();
        var cidade = d.Cidade.Trim();
        if (titulo.Length is < 3 or > 120) throw Invalido("O título deve ter de 3 a 120 caracteres.");
        if (d.Descricao.Length > 2000) throw Invalido("A descrição pode ter no máximo 2000 caracteres.");
        if (!Enum.IsDefined(d.Categoria)) throw Invalido("Escolha uma categoria válida.");
        if (d.Preco is < 1 or > 100000) throw Invalido("O preço deve estar entre R$ 1,00 e R$ 100.000,00.");
        if (d.Estoque is < 1 or > 10000) throw Invalido("O estoque deve ser de 1 a 10.000 unidades.");
        if (cidade.Length is < 2 or > 120) throw Invalido("Informe a cidade.");

        var produto = new Produto
        {
            VendedorId = vendedorId, VendedorNome = vendedorNome, Titulo = titulo, Descricao = d.Descricao.Trim(),
            Categoria = d.Categoria, Preco = decimal.Round(d.Preco, 2), Estoque = d.Estoque, Cidade = cidade
        };

        if (foto is { Length: > 0 })
        {
            var tipo = TipoImagem.Detectar(foto) ?? throw Invalido("Formato de foto não aceito. Use JPEG, PNG ou WebP.");
            produto.Foto = new ProdutoFoto { Dados = foto, ContentType = tipo };
            produto.TemFoto = true;
        }

        await _repositorio.AdicionarProdutoAsync(produto);
        await _repositorio.SalvarAlteracoesAsync();
        return produto;
    }

    /// <summary>O vendedor ajusta preço e estoque ou pausa e reativa o anúncio. Anúncio ainda não aprovado não pode ser reativado.</summary>
    public async Task<Produto> AtualizarProdutoAsync(string vendedorId, int produtoId, decimal? preco, int? estoque, bool? ativo)
    {
        var produto = await ObterDoVendedorAsync(vendedorId, produtoId);

        if (preco is { } p)
        {
            if (p is < 1 or > 100000) throw Invalido("O preço deve estar entre R$ 1,00 e R$ 100.000,00.");
            produto.Preco = decimal.Round(p, 2);
        }

        if (estoque is { } e)
        {
            if (e is < 0 or > 10000) throw Invalido("O estoque deve ser de 0 a 10.000 unidades.");
            produto.Estoque = e;
        }

        if (ativo is { } a)
        {
            if (a && produto.Status == StatusProduto.Inativo) produto.Status = StatusProduto.Aprovado;
            else if (!a && produto.Status == StatusProduto.Aprovado) produto.Status = StatusProduto.Inativo;
            else if (a && produto.Status != StatusProduto.Aprovado) throw new RegraDeNegocioException(TipoErro.Conflito, "Este anúncio ainda não foi aprovado por um moderador.");
        }

        await _repositorio.SalvarAlteracoesAsync();
        return produto;
    }

    public async Task ModerarProdutoAsync(int produtoId, bool aprovar)
    {
        var produto = await _repositorio.ObterProdutoAsync(produtoId) ?? throw NaoEncontrado("Produto");
        if (produto.Status != StatusProduto.Pendente) throw new RegraDeNegocioException(TipoErro.Conflito, "Este anúncio já foi analisado.");
        produto.Status = aprovar ? StatusProduto.Aprovado : StatusProduto.Rejeitado;
        await _repositorio.SalvarAlteracoesAsync();
    }

    // ---------- Comprar ----------
    public async Task<(Pedido Pedido, Pagamento Pagamento)> ComprarAsync(
        string compradorId, string compradorNome, int produtoId, int quantidade, string endereco, string telefone)
    {
        if (!_pagamentos.Habilitado)
        {
            throw new RegraDeNegocioException(TipoErro.Indisponivel, "As compras ainda não estão ativadas nesta plataforma.");
        }

        endereco = endereco.Trim();
        telefone = telefone.Trim();
        if (quantidade is < 1 or > 50) throw Invalido("A quantidade deve ser de 1 a 50.");
        if (endereco.Length is < 10 or > 300) throw Invalido("Informe o endereço de entrega completo.");
        if (telefone.Length is < 8 or > 30) throw Invalido("Informe um telefone para contato.");

        var produto = await _repositorio.ObterProdutoAsync(produtoId);
        if (produto is null || produto.Status != StatusProduto.Aprovado) throw NaoEncontrado("Produto");
        if (produto.VendedorId == compradorId) throw new RegraDeNegocioException(TipoErro.NaoPermitido, "Você não pode comprar o seu próprio produto.");
        if (produto.Estoque < quantidade)
        {
            throw new RegraDeNegocioException(TipoErro.Conflito, produto.Estoque == 0 ? "Produto esgotado." : $"Estoque insuficiente. Restam {produto.Estoque} unidade(s).");
        }

        produto.Estoque -= quantidade; // reserva o estoque; volta se o pedido for cancelado
        var pedido = new Pedido
        {
            ProdutoId = produto.Id, VendedorId = produto.VendedorId, CompradorId = compradorId, CompradorNome = compradorNome,
            TituloProduto = produto.Titulo, PrecoUnitario = produto.Preco, Quantidade = quantidade,
            Total = produto.Preco * quantidade, EnderecoEntrega = endereco, Telefone = telefone
        };
        await _repositorio.AdicionarPedidoAsync(pedido);
        await _repositorio.SalvarAlteracoesAsync();

        try
        {
            var pagamento = await _pagamentos.CriarAsync(TipoPagamento.Pedido, pedido.Id, compradorId, compradorNome,
                pedido.Total, $"Pedido #{pedido.Id}: {produto.Titulo} (x{quantidade})");
            return (pedido, pagamento);
        }
        catch
        {
            produto.Estoque += quantidade;
            pedido.Status = StatusPedido.Cancelado;
            await _repositorio.SalvarAlteracoesAsync();
            throw;
        }
    }

    public Task<IReadOnlyList<Pedido>> ListarComprasAsync(string compradorId) => _repositorio.ListarComprasAsync(compradorId);
    public Task<IReadOnlyList<Pedido>> ListarVendasAsync(string vendedorId) => _repositorio.ListarVendasAsync(vendedorId);

    /// <summary>O comprador desiste de um pedido ainda não pago: o estoque volta e a cobrança é cancelada.</summary>
    public async Task CancelarAsync(string compradorId, int pedidoId)
    {
        var pedido = await _repositorio.ObterPedidoAsync(pedidoId);
        if (pedido is null || pedido.CompradorId != compradorId) throw NaoEncontrado("Pedido");
        if (pedido.Status != StatusPedido.AguardandoPagamento) throw new RegraDeNegocioException(TipoErro.Conflito, "Só é possível cancelar pedidos que ainda não foram pagos.");

        pedido.Status = StatusPedido.Cancelado;
        pedido.AtualizadoEm = DateTime.UtcNow;
        var produto = await _repositorio.ObterProdutoAsync(pedido.ProdutoId);
        if (produto is not null) produto.Estoque += pedido.Quantidade;
        await _repositorio.SalvarAlteracoesAsync();
        await _pagamentos.CancelarPendenteDoAlvoAsync(TipoPagamento.Pedido, pedido.Id);
    }

    public async Task MarcarEnviadoAsync(string vendedorId, int pedidoId)
    {
        var pedido = await _repositorio.ObterPedidoAsync(pedidoId);
        if (pedido is null || pedido.VendedorId != vendedorId) throw NaoEncontrado("Pedido");
        if (pedido.Status != StatusPedido.Pago) throw new RegraDeNegocioException(TipoErro.Conflito, "O pedido precisa estar pago para ser enviado.");

        pedido.Status = StatusPedido.Enviado;
        pedido.AtualizadoEm = DateTime.UtcNow;
        await _repositorio.SalvarAlteracoesAsync();
    }

    public async Task ConfirmarRecebimentoAsync(string compradorId, int pedidoId)
    {
        var pedido = await _repositorio.ObterPedidoAsync(pedidoId);
        if (pedido is null || pedido.CompradorId != compradorId) throw NaoEncontrado("Pedido");
        if (pedido.Status != StatusPedido.Enviado) throw new RegraDeNegocioException(TipoErro.Conflito, "O pedido ainda não foi enviado.");

        pedido.Status = StatusPedido.Entregue;
        pedido.AtualizadoEm = DateTime.UtcNow;
        await _repositorio.SalvarAlteracoesAsync();
    }

    private async Task<Produto> ObterDoVendedorAsync(string vendedorId, int produtoId)
    {
        var produto = await _repositorio.ObterProdutoAsync(produtoId);
        return produto is null || produto.VendedorId != vendedorId ? throw NaoEncontrado("Produto") : produto;
    }

    private static RegraDeNegocioException Invalido(string mensagem) => new(TipoErro.Invalido, mensagem);
    private static RegraDeNegocioException NaoEncontrado(string o) => new(TipoErro.NaoEncontrado, $"{o} não encontrado.");
}

/// <summary>Quando o pagamento de um pedido é confirmado, o pedido passa a Pago.</summary>
public class PedidoPagamentoHandler : IPagamentoConfirmadoHandler
{
    private readonly ILojaRepository _repositorio;

    public PedidoPagamentoHandler(ILojaRepository repositorio) => _repositorio = repositorio;

    public TipoPagamento Tipo => TipoPagamento.Pedido;

    public async Task AoConfirmarAsync(Pagamento pagamento, CancellationToken ct = default)
    {
        var pedido = await _repositorio.ObterPedidoAsync(pagamento.AlvoId);
        if (pedido is { Status: StatusPedido.AguardandoPagamento })
        {
            pedido.Status = StatusPedido.Pago;
            pedido.AtualizadoEm = DateTime.UtcNow;
        }
    }
}
