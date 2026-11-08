namespace TechFlow.Eventos.Core.Recomendacoes;

/// <summary>Interesses escolhidos por uma pessoa logada (categorias e cidades). Vazio = sem preferência.</summary>
public class PreferenciasUsuario
{
    public string UsuarioId { get; set; } = string.Empty;
    public int[] Categorias { get; set; } = Array.Empty<int>();
    public string[] Cidades { get; set; } = Array.Empty<string>();
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

public interface IPreferenciasRepository
{
    Task<PreferenciasUsuario?> ObterAsync(string usuarioId);

    /// <summary>Cria ou substitui as preferências do usuário.</summary>
    Task SalvarAsync(string usuarioId, int[] categorias, string[] cidades);
}
