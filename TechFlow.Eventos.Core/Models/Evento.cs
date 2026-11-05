using TechFlow.Eventos.Core.Enums;

namespace TechFlow.Eventos.Core.Models;

/// <summary>
/// Evento da agenda. Enviado por organizadores ou moradores (QR code → "Divulgar evento")
/// e publicado somente depois da aprovação de um moderador.
/// </summary>
public class Evento
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public CategoriaEvento Categoria { get; set; }
    public string Cidade { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;

    /// <summary>Opcionais. Sem coordenadas o evento não entra no cálculo de distância.</summary>
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateOnly Data { get; set; }
    public TimeOnly Horario { get; set; }
    public string Atracoes { get; set; } = string.Empty;

    /// <summary>Valor em reais. Zero significa gratuito.</summary>
    public decimal Preco { get; set; }
    public string Acessibilidade { get; set; } = string.Empty;

    /// <summary>Onde comprar ou como se inscrever (texto livre ou link).</summary>
    public string Ingressos { get; set; } = string.Empty;
    public string Organizador { get; set; } = string.Empty;

    /// <summary>Site ou perfil oficial que confirma o evento.</summary>
    public string FonteOficial { get; set; } = string.Empty;

    /// <summary>Contato de quem enviou. Visível apenas para moderadores.</summary>
    public string ContatoEnvio { get; set; } = string.Empty;
    public TipoAutorEvento TipoAutor { get; set; } = TipoAutorEvento.Morador;

    /// <summary>Definido pelo moderador; aparece em "Em destaque" na página inicial.</summary>
    public bool Destaque { get; set; }
    public StatusEvento Status { get; set; } = StatusEvento.Pendente;
    public DateTime DataEnvio { get; set; } = DateTime.UtcNow;

    /// <summary>Id do usuário logado que enviou (nulo quando o envio é anônimo).</summary>
    public string? UsuarioId { get; set; }

    public bool Gratuito => Preco <= 0;

    /// <summary>Data de hoje em Brasília (UTC-3), usada para esconder eventos que já passaram.</summary>
    public static DateOnly HojeBrasil() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));
}
