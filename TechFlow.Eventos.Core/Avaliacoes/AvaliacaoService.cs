using System.Text.RegularExpressions;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Core.Avaliacoes;

/// <summary>
/// Regras do módulo Avaliações. Só eventos aprovados e que já começaram podem ser avaliados.
/// Nota sem comentário é publicada na hora; comentário passa pela moderação.
/// </summary>
public class AvaliacaoService
{
    private const int MaxComentario = 1000;
    private const int MaxNome = 60;
    private static readonly Regex Link = new(@"(https?://|www\.)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IAvaliacaoRepository _avaliacoes;
    private readonly IEventoRepository _eventos;

    public AvaliacaoService(IAvaliacaoRepository avaliacoes, IEventoRepository eventos)
    {
        _avaliacoes = avaliacoes;
        _eventos = eventos;
    }

    public async Task<RespostaAvaliacao> EnviarAsync(int eventoId, string? nome, int nota, string? comentario)
    {
        if (nota is < 1 or > 5) return Invalida("A nota deve ser de 1 a 5.");

        var texto = (comentario ?? string.Empty).Trim();
        if (texto.Length > MaxComentario) return Invalida($"O comentário pode ter no máximo {MaxComentario} caracteres.");
        if (Link.IsMatch(texto)) return Invalida("Não é permitido incluir links no comentário.");

        var evento = await _eventos.ObterPorIdAsync(eventoId);
        if (evento is null || evento.Status != StatusEvento.Aprovado)
        {
            return new RespostaAvaliacao(ResultadoAvaliacao.EventoNaoEncontrado);
        }

        if (evento.Data > Evento.HojeBrasil())
        {
            return new RespostaAvaliacao(ResultadoAvaliacao.EventoAindaNaoAconteceu, "Você poderá avaliar este evento depois que ele acontecer.");
        }

        var nomeLimpo = (nome ?? string.Empty).Trim();
        if (nomeLimpo.Length > MaxNome) nomeLimpo = nomeLimpo[..MaxNome];

        var publicar = texto.Length == 0;
        await _avaliacoes.AdicionarAsync(new Avaliacao
        {
            EventoId = eventoId,
            Nome = nomeLimpo.Length == 0 ? "Visitante" : nomeLimpo,
            Nota = nota,
            Comentario = texto,
            Status = publicar ? StatusAvaliacao.Aprovada : StatusAvaliacao.Pendente
        });
        await _avaliacoes.SalvarAlteracoesAsync();

        return new RespostaAvaliacao(ResultadoAvaliacao.Recebida, Publicada: publicar);
    }

    public Task<IReadOnlyList<Avaliacao>> ListarAsync(int eventoId, int limite = 50) =>
        _avaliacoes.ListarAprovadasAsync(eventoId, Math.Clamp(limite, 1, 100));

    public async Task<ResumoAvaliacoes> ResumoAsync(int eventoId) =>
        (await _avaliacoes.ResumirAsync(new[] { eventoId })).FirstOrDefault() ?? new ResumoAvaliacoes(eventoId, 0, 0);

    public Task<IReadOnlyList<ResumoAvaliacoes>> ResumirAsync(IReadOnlyCollection<int> eventoIds) => _avaliacoes.ResumirAsync(eventoIds);

    public Task<IReadOnlyList<AvaliacaoPendente>> ListarPendentesAsync() => _avaliacoes.ListarPendentesAsync();

    public async Task<bool> ModerarAsync(int id, bool aprovar)
    {
        var avaliacao = await _avaliacoes.ObterPorIdAsync(id);
        if (avaliacao is null) return false;

        avaliacao.Status = aprovar ? StatusAvaliacao.Aprovada : StatusAvaliacao.Rejeitada;
        await _avaliacoes.SalvarAlteracoesAsync();
        return true;
    }

    private static RespostaAvaliacao Invalida(string mensagem) => new(ResultadoAvaliacao.Invalida, mensagem);
}
