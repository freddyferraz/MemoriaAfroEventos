using System.Security.Cryptography;
using TechFlow.Eventos.Core.Compartilhado;

namespace TechFlow.Eventos.Core.Pagamentos;

public class PagamentoService
{
    private readonly IPagamentoRepository _repositorio;
    private readonly IPagamentoGateway? _gateway;
    private readonly IEnumerable<IPagamentoConfirmadoHandler> _handlers;

    // O gateway é opcional: sem ele (modo Desativado) nenhuma cobrança pode ser criada.
    public PagamentoService(IPagamentoRepository repositorio, IEnumerable<IPagamentoConfirmadoHandler> handlers, IPagamentoGateway? gateway = null)
    {
        _repositorio = repositorio;
        _handlers = handlers;
        _gateway = gateway;
    }

    public bool Habilitado => _gateway is not null;
    public bool PermiteSimulacao => _gateway?.PermiteSimulacao ?? false;
    public string? Instrucoes => _gateway?.Instrucoes;

    public async Task<Pagamento> CriarAsync(TipoPagamento tipo, int alvoId, string usuarioId, string usuarioNome, decimal valor, string descricao)
    {
        if (_gateway is null)
        {
            throw new RegraDeNegocioException(TipoErro.Indisponivel, "Os pagamentos ainda não estão ativados nesta plataforma.");
        }

        if (valor <= 0) throw new RegraDeNegocioException(TipoErro.Invalido, "O valor da cobrança deve ser maior que zero.");

        var pagamento = new Pagamento
        {
            Referencia = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant(),
            Tipo = tipo, AlvoId = alvoId, UsuarioId = usuarioId, UsuarioNome = usuarioNome,
            Valor = valor, Descricao = descricao, Gateway = _gateway.Nome
        };

        await _repositorio.AdicionarAsync(pagamento);
        await _repositorio.SalvarAlteracoesAsync();
        return pagamento;
    }

    public Task<Pagamento?> ObterAsync(string referencia) => _repositorio.ObterPorReferenciaAsync(referencia);

    public Task<Pagamento?> ObterPendenteDoAlvoAsync(TipoPagamento tipo, int alvoId) => _repositorio.ObterPendentePorAlvoAsync(tipo, alvoId);

    public Task<IReadOnlyList<Pagamento>> ListarPendentesAsync() => _repositorio.ListarPendentesAsync();

    /// <summary>Marca como pago e deixa cada módulo reagir. Idempotente: confirmar de novo não repete os efeitos.</summary>
    public async Task<bool> ConfirmarAsync(string referencia, CancellationToken ct = default)
    {
        var pagamento = await _repositorio.ObterPorReferenciaAsync(referencia);
        if (pagamento is null) return false;
        if (pagamento.Status == StatusPagamento.Pago) return true;
        if (pagamento.Status == StatusPagamento.Cancelado)
        {
            throw new RegraDeNegocioException(TipoErro.Conflito, "Esta cobrança foi cancelada.");
        }

        pagamento.Status = StatusPagamento.Pago;
        pagamento.PagoEm = DateTime.UtcNow;
        foreach (var handler in _handlers.Where(h => h.Tipo == pagamento.Tipo))
        {
            await handler.AoConfirmarAsync(pagamento, ct);
        }

        await _repositorio.SalvarAlteracoesAsync(); // pagamento + efeitos dos módulos na mesma gravação
        return true;
    }

    public async Task CancelarPendenteDoAlvoAsync(TipoPagamento tipo, int alvoId)
    {
        var pagamento = await _repositorio.ObterPendentePorAlvoAsync(tipo, alvoId);
        if (pagamento is null) return;
        pagamento.Status = StatusPagamento.Cancelado;
        await _repositorio.SalvarAlteracoesAsync();
    }
}
