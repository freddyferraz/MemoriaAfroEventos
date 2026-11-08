using TechFlow.Eventos.Core.Compartilhado;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Pagamentos;

namespace TechFlow.Eventos.Core.Planos;

public class PlanoService : ICotaDestaque
{
    public const int DiasPorAssinatura = 30;

    private readonly IAssinaturaPlanoRepository _repositorio;
    private readonly IEventoRepository _eventos;
    private readonly PagamentoService _pagamentos;

    public PlanoService(IAssinaturaPlanoRepository repositorio, IEventoRepository eventos, PagamentoService pagamentos)
    {
        _repositorio = repositorio;
        _eventos = eventos;
        _pagamentos = pagamentos;
    }

    /// <summary>O plano em vigor é o mais alto entre as assinaturas pagas ainda válidas; sem nenhuma, vale o Free.</summary>
    public async Task<PlanoAtual> ObterPlanoAtualAsync(string usuarioId)
    {
        var ativas = await _repositorio.ListarAtivasAsync(usuarioId, DateTime.UtcNow);
        var melhor = ativas.OrderByDescending(a => (int)a.Plano).ThenByDescending(a => a.ValidaAte).FirstOrDefault();
        return melhor is null
            ? new PlanoAtual(CatalogoPlanos.Free, null)
            : new PlanoAtual(CatalogoPlanos.Obter(melhor.Plano) ?? CatalogoPlanos.Free, melhor.ValidaAte);
    }

    /// <summary>O mês da cota é o mês civil em Brasília (UTC-3): ela renova no dia 1º.</summary>
    public async Task<CotaDestaque> ObterCotaAsync(string usuarioId)
    {
        var atual = await ObterPlanoAtualAsync(usuarioId);
        var agoraLocal = DateTime.UtcNow.AddHours(-3);
        var inicioLocal = new DateTime(agoraLocal.Year, agoraLocal.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var inicioUtc = DateTime.SpecifyKind(inicioLocal.AddHours(3), DateTimeKind.Utc);
        var fimUtc = DateTime.SpecifyKind(inicioLocal.AddMonths(1).AddHours(3), DateTimeKind.Utc);

        var usados = await _eventos.ContarDestaquesSolicitadosAsync(usuarioId, inicioUtc, fimUtc);
        var limite = atual.Plano.DestaquesPorMes;
        return new CotaDestaque(atual.Plano.Nome, limite, usados, Math.Max(0, limite - usados), fimUtc);
    }

    public async Task<Pagamento> ContratarAsync(string usuarioId, string usuarioNome, PlanoId id)
    {
        var plano = CatalogoPlanos.Obter(id) ?? throw new RegraDeNegocioException(TipoErro.Invalido, "Plano inválido.");
        if (plano.PrecoMensal <= 0) throw new RegraDeNegocioException(TipoErro.Invalido, "O plano Free já está incluído na sua conta; não precisa contratar.");
        if (!_pagamentos.Habilitado) throw new RegraDeNegocioException(TipoErro.Indisponivel, "As assinaturas ainda não estão ativadas nesta plataforma.");

        var assinatura = new AssinaturaPlano { UsuarioId = usuarioId, Plano = id };
        await _repositorio.AdicionarAsync(assinatura);
        await _repositorio.SalvarAlteracoesAsync();

        return await _pagamentos.CriarAsync(TipoPagamento.Assinatura, assinatura.Id, usuarioId, usuarioNome,
            plano.PrecoMensal, $"Plano {plano.Nome}: {DiasPorAssinatura} dias");
    }
}

/// <summary>Quando o pagamento de uma assinatura é confirmado, ela passa a valer por 30 dias (somados ao que restar do mesmo plano).</summary>
public class AssinaturaPagamentoHandler : IPagamentoConfirmadoHandler
{
    private readonly IAssinaturaPlanoRepository _repositorio;

    public AssinaturaPagamentoHandler(IAssinaturaPlanoRepository repositorio) => _repositorio = repositorio;

    public TipoPagamento Tipo => TipoPagamento.Assinatura;

    public async Task AoConfirmarAsync(Pagamento pagamento, CancellationToken ct = default)
    {
        var assinatura = await _repositorio.ObterAsync(pagamento.AlvoId);
        if (assinatura is null || assinatura.Status == StatusAssinatura.Ativa) return;

        var agora = DateTime.UtcNow;
        var restante = (await _repositorio.ListarAtivasAsync(assinatura.UsuarioId, agora))
            .Where(a => a.Plano == assinatura.Plano && a.ValidaAte.HasValue)
            .Select(a => a.ValidaAte!.Value)
            .DefaultIfEmpty(agora)
            .Max();

        assinatura.Status = StatusAssinatura.Ativa;
        assinatura.InicioEm = agora;
        assinatura.ValidaAte = (restante > agora ? restante : agora).AddDays(PlanoService.DiasPorAssinatura);
    }
}
