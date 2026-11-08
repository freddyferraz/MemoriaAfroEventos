using System.ComponentModel.DataAnnotations;

namespace TechFlow.Eventos.Api.DTOs;

// ---------- Avaliações ----------
public class AvaliarEventoDto
{
    [Range(1, int.MaxValue)] public int EventoId { get; set; }
    [MaxLength(60)] public string? Nome { get; set; }
    [Range(1, 5)] public int Nota { get; set; }
    [MaxLength(1000)] public string? Comentario { get; set; }

    /// <summary>Campo isca: pessoas não o veem; robôs costumam preenchê-lo. Se vier preenchido, o envio é descartado.</summary>
    public string? Website { get; set; }
}

public class ModerarAvaliacaoDto
{
    public bool Aprovar { get; set; }
}

// ---------- Recomendações ----------
public class PesoCategoriaDto
{
    public int Categoria { get; set; }
    [Range(0, 1000)] public double Peso { get; set; }
}

public class RecomendacaoRequestDto
{
    [MaxLength(10)] public List<PesoCategoriaDto>? Categorias { get; set; }
    [MaxLength(20)] public List<string>? Cidades { get; set; }
    [MaxLength(100)] public List<int>? ExcluirIds { get; set; }
    [Range(1, 12)] public int Limite { get; set; } = 6;
}

public class InteressesDto
{
    [MaxLength(10)] public List<int>? Categorias { get; set; }
    [MaxLength(20)] public List<string>? Cidades { get; set; }
}

public class EventoRecomendadoDto : EventoDto
{
    public string Motivo { get; set; } = string.Empty;
}
