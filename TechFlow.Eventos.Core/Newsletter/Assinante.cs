using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Core.Newsletter;

/// <summary>Pessoa inscrita na newsletter, com as preferências de categoria e cidade.</summary>
public class Assinante
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>Valores de CategoriaEvento. Vazio significa "todas as categorias".</summary>
    public int[] Categorias { get; set; } = Array.Empty<int>();

    /// <summary>Vazio significa "todas as cidades".</summary>
    public string[] Cidades { get; set; } = Array.Empty<string>();

    /// <summary>Token aleatório usado nos links de confirmação e de cancelamento enviados por e-mail.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Só assinantes confirmados (double opt-in) recebem eventos.</summary>
    public bool Confirmado { get; set; }

    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    /// <summary>Quando a pessoa marcou o aceite (LGPD) no formulário.</summary>
    public DateTime? DataAceite { get; set; }
    public DateTime? ConfirmacaoEnviadaEm { get; set; }
    public DateTime? DataConfirmacao { get; set; }

    public bool QuerReceber(Evento e) =>
        Confirmado
        && (Categorias.Length == 0 || Categorias.Contains((int)e.Categoria))
        && (Cidades.Length == 0 || Cidades.Contains(e.Cidade, StringComparer.OrdinalIgnoreCase));
}

/// <summary>Registro de que o evento já foi enviado a um assinante (impede e-mail duplicado).</summary>
public class EnvioNewsletter
{
    public int Id { get; set; }
    public int EventoId { get; set; }
    public int AssinanteId { get; set; }
    public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;
}

public record EmailMensagem(string Para, string Assunto, string Html, string Texto);

public record ResultadoEnvio(int Destinatarios, int Enviados, int Falhas, string? UltimoErro);

public class NewsletterOptions
{
    /// <summary>Endereço público do site (ex.: https://meusite.onrender.com), usado nos links dos e-mails.</summary>
    public string BaseUrl { get; set; } = string.Empty;
    public string NomeRemetente { get; set; } = "TechFlow Eventos";
    public string EmailRemetente { get; set; } = string.Empty;

    public string BaseUrlLimpa => BaseUrl.TrimEnd('/');
}
