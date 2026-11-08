namespace TechFlow.Eventos.Core.Planos;

public enum PlanoId
{
    Free = 1,
    Basico = 2,
    Intermediario = 3,
    Avancado = 4
}

public record Plano(PlanoId Id, string Nome, int DestaquesPorMes, decimal PrecoMensal);

/// <summary>Planos oferecidos aos organizadores. Para mudar preços ou cotas, altere esta lista.</summary>
public static class CatalogoPlanos
{
    public static readonly IReadOnlyList<Plano> Todos = new[]
    {
        new Plano(PlanoId.Free, "Free", 1, 0m),
        new Plano(PlanoId.Basico, "Básico", 2, 20m),
        new Plano(PlanoId.Intermediario, "Intermediário", 5, 40m),
        new Plano(PlanoId.Avancado, "Avançado", 10, 80m)
    };

    public static Plano Free => Todos[0];

    public static Plano? Obter(PlanoId id) => Todos.FirstOrDefault(p => p.Id == id);
}

public enum StatusAssinatura
{
    AguardandoPagamento = 1,
    Ativa = 2
}

/// <summary>Contratação de um plano pago por 30 dias. A validade conta a partir da confirmação do pagamento.</summary>
public class AssinaturaPlano
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public PlanoId Plano { get; set; }
    public StatusAssinatura Status { get; set; } = StatusAssinatura.AguardandoPagamento;
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? InicioEm { get; set; }
    public DateTime? ValidaAte { get; set; }
}

public record PlanoAtual(Plano Plano, DateTime? ValidaAte);

/// <summary>Destaques do mês: limite do plano, quantos já foram pedidos e quantos restam.</summary>
public record CotaDestaque(string Plano, int Limite, int Usado, int Disponivel, DateTime RenovaEmUtc);

/// <summary>Contrato usado pelo módulo de Eventos para saber se a pessoa ainda pode pedir destaque. O módulo de Planos o implementa.</summary>
public interface ICotaDestaque
{
    Task<CotaDestaque> ObterCotaAsync(string usuarioId);
}

public interface IAssinaturaPlanoRepository
{
    Task AdicionarAsync(AssinaturaPlano assinatura);
    Task<AssinaturaPlano?> ObterAsync(int id);
    Task<IReadOnlyList<AssinaturaPlano>> ListarAtivasAsync(string usuarioId, DateTime agoraUtc);
    Task SalvarAlteracoesAsync();
}
