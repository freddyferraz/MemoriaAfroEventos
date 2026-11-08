using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Core.Recomendacoes;

public record PesoCategoria(int Categoria, double Peso);

/// <summary>Interesses informados pelo próprio navegador do visitante. O servidor não guarda este perfil.</summary>
public record PerfilInteresses(IReadOnlyList<PesoCategoria> Categorias, IReadOnlyList<string> Cidades)
{
    public static PerfilInteresses Vazio { get; } = new(Array.Empty<PesoCategoria>(), Array.Empty<string>());

    public bool EstaVazio => Categorias.Count == 0 && Cidades.Count == 0;
}

public record EventoRecomendado(Evento Evento, double Pontos, string Motivo);

/// <summary>
/// Recomendação por pontuação simples e explicável: categoria de interesse (peso relativo), cidade de interesse,
/// destaque e proximidade da data. Sem perfil, devolve os eventos em destaque e os mais próximos.
/// </summary>
public class RecomendacaoService
{
    private readonly IEventoRepository _eventos;

    /// <summary>Peso de uma categoria marcada pela própria pessoa, comparado a um clique (peso 1).</summary>
    public const double PesoEscolha = 5;

    public RecomendacaoService(IEventoRepository eventos) => _eventos = eventos;

    /// <summary>Junta os interesses escolhidos (guardados na conta) com os sinais de uso enviados pelo navegador.</summary>
    public static PerfilInteresses Mesclar(PerfilInteresses sinais, PreferenciasUsuario? escolhas)
    {
        if (escolhas is null) return sinais;

        var pesos = sinais.Categorias.GroupBy(c => c.Categoria).ToDictionary(g => g.Key, g => g.Sum(x => x.Peso));
        foreach (var c in escolhas.Categorias) pesos[c] = pesos.GetValueOrDefault(c) + PesoEscolha;

        var cidades = sinais.Cidades.Concat(escolhas.Cidades).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new PerfilInteresses(pesos.Select(kv => new PesoCategoria(kv.Key, kv.Value)).ToList(), cidades);
    }

    public async Task<IReadOnlyList<EventoRecomendado>> RecomendarAsync(PerfilInteresses perfil, IEnumerable<int> excluirIds, int limite)
    {
        var excluir = excluirIds.ToHashSet();
        var candidatos = (await _eventos.BuscarAprovadosAsync(new EventoFiltro())).Where(e => !excluir.Contains(e.Id));
        var hoje = Evento.HojeBrasil();
        var maiorPeso = perfil.Categorias.Count == 0 ? 1 : Math.Max(perfil.Categorias.Max(c => c.Peso), 0.0001);

        return candidatos
            .Select(e =>
            {
                var peso = perfil.Categorias.Where(c => c.Categoria == (int)e.Categoria).Select(c => c.Peso).DefaultIfEmpty(0).Max();
                var pontosCategoria = peso <= 0 ? 0 : 5 * Math.Min(peso / maiorPeso, 1);
                var cidadeCombina = perfil.Cidades.Any(c => string.Equals(c, e.Cidade, StringComparison.OrdinalIgnoreCase));
                var pontos = pontosCategoria + (cidadeCombina ? 2 : 0) + (e.Destaque ? 0.5 : 0) + Proximidade(e, hoje);

                var motivo = pontosCategoria > 0 ? $"Porque você se interessa por {e.Categoria.Nome().ToLowerInvariant()}"
                    : cidadeCombina ? $"Perto de você: {e.Cidade}"
                    : e.Destaque ? "Em destaque"
                    : "Acontece em breve";

                return new EventoRecomendado(e, pontos, motivo);
            })
            .OrderByDescending(r => r.Pontos).ThenBy(r => r.Evento.Data).ThenBy(r => r.Evento.Horario)
            .Take(Math.Clamp(limite, 1, 12))
            .ToList();
    }

    /// <summary>Eventos parecidos com um evento específico: mesma categoria, mesma cidade e datas próximas.</summary>
    public async Task<IReadOnlyList<EventoRecomendado>> SimilaresAsync(int eventoId, int limite)
    {
        var baseEvento = await _eventos.ObterPorIdAsync(eventoId);
        if (baseEvento is null) return Array.Empty<EventoRecomendado>();

        var hoje = Evento.HojeBrasil();
        return (await _eventos.BuscarAprovadosAsync(new EventoFiltro()))
            .Where(e => e.Id != eventoId)
            .Select(e =>
            {
                var mesmaCategoria = e.Categoria == baseEvento.Categoria;
                var mesmaCidade = string.Equals(e.Cidade, baseEvento.Cidade, StringComparison.OrdinalIgnoreCase);
                var pontos = (mesmaCategoria ? 4 : 0) + (mesmaCidade ? 2 : 0) + Proximidade(e, hoje);
                var motivo = mesmaCategoria && mesmaCidade ? $"{e.Categoria.Nome()} em {e.Cidade}"
                    : mesmaCategoria ? $"Mais de {e.Categoria.Nome().ToLowerInvariant()}"
                    : mesmaCidade ? $"Também em {e.Cidade}"
                    : "Acontece em breve";
                return new EventoRecomendado(e, pontos, motivo);
            })
            .Where(r => r.Pontos >= 2)
            .OrderByDescending(r => r.Pontos).ThenBy(r => r.Evento.Data)
            .Take(Math.Clamp(limite, 1, 12))
            .ToList();
    }

    private static double Proximidade(Evento e, DateOnly hoje)
    {
        var dias = e.Data.DayNumber - hoje.DayNumber;
        return dias <= 7 ? 1.5 : dias <= 30 ? 0.75 : 0;
    }
}
