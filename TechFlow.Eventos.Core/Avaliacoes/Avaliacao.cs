namespace TechFlow.Eventos.Core.Avaliacoes;

public enum StatusAvaliacao
{
    Pendente = 1,
    Aprovada = 2,
    Rejeitada = 3
}

/// <summary>Avaliação (nota de 1 a 5) e comentário opcional sobre um evento que já aconteceu.</summary>
public class Avaliacao
{
    public int Id { get; set; }
    public int EventoId { get; set; }
    public string Nome { get; set; } = "Visitante";
    public int Nota { get; set; }
    public string Comentario { get; set; } = string.Empty;
    public StatusAvaliacao Status { get; set; } = StatusAvaliacao.Pendente;
    public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
}

public record ResumoAvaliacoes(int EventoId, double Media, int Total);

public record AvaliacaoPendente(Avaliacao Avaliacao, string EventoTitulo);

public enum ResultadoAvaliacao
{
    Recebida,
    EventoNaoEncontrado,
    EventoAindaNaoAconteceu,
    Invalida
}

public record RespostaAvaliacao(ResultadoAvaliacao Resultado, string? Mensagem = null, bool Publicada = false);
