namespace TechFlow.Eventos.Core.Pagamentos;

public enum TipoPagamento
{
    Pedido = 1,
    Assinatura = 2
}

public enum StatusPagamento
{
    Pendente = 1,
    Pago = 2,
    Cancelado = 3
}

/// <summary>Cobrança de um pedido da loja ou de uma assinatura de plano. A Referencia identifica a cobrança em links e no gateway.</summary>
public class Pagamento
{
    public int Id { get; set; }
    public string Referencia { get; set; } = string.Empty;
    public TipoPagamento Tipo { get; set; }

    /// <summary>Id do pedido ou da assinatura, conforme o Tipo.</summary>
    public int AlvoId { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public string UsuarioNome { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public StatusPagamento Status { get; set; } = StatusPagamento.Pendente;
    public string Gateway { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? PagoEm { get; set; }

    public string UrlPagamento => $"/pagamento.html?ref={Referencia}";
}

public interface IPagamentoRepository
{
    Task AdicionarAsync(Pagamento pagamento);
    Task<Pagamento?> ObterPorReferenciaAsync(string referencia);
    Task<Pagamento?> ObterPendentePorAlvoAsync(TipoPagamento tipo, int alvoId);
    Task<IReadOnlyList<Pagamento>> ListarPendentesAsync();
    Task SalvarAlteracoesAsync();
}

/// <summary>
/// Forma de receber. Hoje há dois: Simulado (demonstração, sem cobrança real) e Manual (a pessoa paga por fora, por exemplo via Pix,
/// e um administrador confirma). Um gateway real (Mercado Pago, Stripe...) entra implementando esta interface e chamando
/// <see cref="PagamentoService.ConfirmarAsync"/> a partir do webhook do provedor.
/// </summary>
public interface IPagamentoGateway
{
    string Nome { get; }

    /// <summary>Se verdadeiro, a própria pessoa pode "aprovar" o pagamento na tela (só para demonstração).</summary>
    bool PermiteSimulacao { get; }

    /// <summary>Texto mostrado na tela de pagamento (ex.: chave Pix e como enviar o comprovante).</summary>
    string? Instrucoes { get; }
}

/// <summary>Quem precisa reagir a um pagamento confirmado (loja, planos) implementa isto. Os handlers só alteram entidades; o serviço salva tudo de uma vez.</summary>
public interface IPagamentoConfirmadoHandler
{
    TipoPagamento Tipo { get; }

    Task AoConfirmarAsync(Pagamento pagamento, CancellationToken ct = default);
}
