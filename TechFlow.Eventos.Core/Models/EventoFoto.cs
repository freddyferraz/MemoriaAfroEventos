namespace TechFlow.Eventos.Core.Models;

/// <summary>
/// Foto do evento, guardada no banco (bytea). Fica em tabela própria para que as listagens
/// de eventos não carreguem os bytes da imagem.
/// </summary>
public class EventoFoto
{
    public int EventoId { get; set; }
    public Evento? Evento { get; set; }

    public byte[] Dados { get; set; } = Array.Empty<byte>();

    /// <summary>image/jpeg, image/png ou image/webp (detectado pelo conteúdo, não pelo nome do arquivo).</summary>
    public string ContentType { get; set; } = string.Empty;
}
