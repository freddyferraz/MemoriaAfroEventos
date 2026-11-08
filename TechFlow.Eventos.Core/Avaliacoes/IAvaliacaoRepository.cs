namespace TechFlow.Eventos.Core.Avaliacoes;

public interface IAvaliacaoRepository
{
    Task AdicionarAsync(Avaliacao avaliacao);
    Task<Avaliacao?> ObterPorIdAsync(int id);
    Task<IReadOnlyList<Avaliacao>> ListarAprovadasAsync(int eventoId, int limite);
    Task<IReadOnlyList<ResumoAvaliacoes>> ResumirAsync(IReadOnlyCollection<int> eventoIds);
    Task<IReadOnlyList<AvaliacaoPendente>> ListarPendentesAsync();
    Task SalvarAlteracoesAsync();
}
