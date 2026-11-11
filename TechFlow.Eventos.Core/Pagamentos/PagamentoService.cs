using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TechFlow.Eventos.Core.Compartilhado;

namespace TechFlow.Eventos.Core.Pagamentos;

public enum SituacaoTentativa
{
    Aprovado,
    AguardandoPagamento,
    Recusado
}

public record ResultadoTentativa(SituacaoTentativa Situacao, string Mensagem);

public class PagamentoService
{
    private static readonly Regex QuatroDigitos = new(@"^\d{4}$", RegexOptions.Compiled);

    /// <summary>Cartão de teste: terminado em 0002, o emissor "recusa" o pagamento (para demonstrar o erro).</summary>
    public const string FinalCartaoRecusado = "0002";

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

    /// <summary>
    /// Escolha da forma de pagamento (modo Simulado). Cartão: aprova na hora, exceto o cartão de teste de recusa.
    /// Pix e boleto: geram um código fictício e ficam aguardando a "confirmação" (simulada) na tela.
    /// O número do cartão não chega ao servidor, como acontece com gateways reais: só os 4 últimos dígitos.
    /// </summary>
    public async Task<ResultadoTentativa> TentarPagarAsync(string referencia, string usuarioId, MetodoPagamento metodo, int? parcelas, string? finalCartao, CancellationToken ct = default)
    {
        if (!PermiteSimulacao) throw new RegraDeNegocioException(TipoErro.NaoEncontrado, "Pagamento não encontrado.");

        var p = await _repositorio.ObterPorReferenciaAsync(referencia);
        if (p is null || p.UsuarioId != usuarioId) throw new RegraDeNegocioException(TipoErro.NaoEncontrado, "Pagamento não encontrado.");
        if (p.Status != StatusPagamento.Pendente) throw new RegraDeNegocioException(TipoErro.Conflito, "Esta cobrança já foi finalizada.");

        switch (metodo)
        {
            case MetodoPagamento.Pix:
                p.Metodo = metodo; p.Parcelas = 1; p.FinalCartao = null; p.CodigoBoleto = null;
                p.VenceEm = DateTime.UtcNow.AddMinutes(30);
                p.CodigoPix = SimulacaoPagamento.CodigoPix(p);
                await _repositorio.SalvarAlteracoesAsync();
                return new ResultadoTentativa(SituacaoTentativa.AguardandoPagamento, "Pix gerado. Pague pelo aplicativo do seu banco em até 30 minutos (simulação).");

            case MetodoPagamento.Boleto:
                p.Metodo = metodo; p.Parcelas = 1; p.FinalCartao = null; p.CodigoPix = null;
                p.VenceEm = DateTime.UtcNow.AddDays(3);
                p.CodigoBoleto = SimulacaoPagamento.LinhaDigitavel(p);
                await _repositorio.SalvarAlteracoesAsync();
                return new ResultadoTentativa(SituacaoTentativa.AguardandoPagamento, "Boleto gerado, com vencimento em 3 dias. A compensação leva até 2 dias úteis (simulação).");

            default:
                if (finalCartao is null || !QuatroDigitos.IsMatch(finalCartao)) throw new RegraDeNegocioException(TipoErro.Invalido, "Informe um cartão válido.");
                var numParcelas = metodo == MetodoPagamento.CartaoDebito ? 1 : parcelas ?? 1;
                if (numParcelas < 1 || numParcelas > Parcelamento.Maximo(p.Valor))
                {
                    throw new RegraDeNegocioException(TipoErro.Invalido, "Número de parcelas inválido para este valor.");
                }

                if (finalCartao == FinalCartaoRecusado)
                {
                    throw new RegraDeNegocioException(TipoErro.Invalido, "Pagamento recusado pelo emissor do cartão (cartão de teste de recusa). Tente outro cartão ou outra forma de pagamento.");
                }

                p.Metodo = metodo; p.Parcelas = numParcelas; p.FinalCartao = finalCartao; p.CodigoPix = null; p.CodigoBoleto = null; p.VenceEm = null;
                await ConfirmarAsync(referencia, ct);
                return new ResultadoTentativa(SituacaoTentativa.Aprovado, "Pagamento aprovado.");
        }
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
