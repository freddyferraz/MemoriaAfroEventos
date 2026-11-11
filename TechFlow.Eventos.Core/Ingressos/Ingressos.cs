using TechFlow.Eventos.Core.Compartilhado;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Models;
using TechFlow.Eventos.Core.Pagamentos;
using System.Security.Cryptography;

namespace TechFlow.Eventos.Core.Ingressos;

public enum StatusIngresso
{
    AguardandoPagamento = 1,
    Confirmado = 2,
    Cancelado = 3
}

/// <summary>Compra de ingressos de um evento. Uma compra pode ter vários ingressos e um código de apresentação (QR) na entrada.</summary>
public class CompraIngresso
{
    public int Id { get; set; }
    public int EventoId { get; set; }
    public string CompradorId { get; set; } = string.Empty;
    public string CompradorNome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public StatusIngresso Status { get; set; } = StatusIngresso.AguardandoPagamento;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmadoEm { get; set; }
}

public record IngressoDoUsuario(CompraIngresso Compra, string EventoTitulo, DateOnly Data, TimeOnly Horario, string Local);

public interface IIngressoRepository
{
    Task AdicionarAsync(CompraIngresso compra);
    Task<CompraIngresso?> ObterAsync(int id);
    Task<IReadOnlyList<IngressoDoUsuario>> ListarDoUsuarioAsync(string usuarioId);
    Task SalvarAlteracoesAsync();
}

/// <summary>Venda de ingressos pela plataforma (demonstração). Exige conta; eventos gratuitos geram o ingresso na hora, sem pagamento.</summary>
public class IngressoService
{
    private const int MaximoPorCompra = 10;

    private readonly IIngressoRepository _repositorio;
    private readonly IEventoRepository _eventos;
    private readonly PagamentoService _pagamentos;

    public IngressoService(IIngressoRepository repositorio, IEventoRepository eventos, PagamentoService pagamentos)
    {
        _repositorio = repositorio;
        _eventos = eventos;
        _pagamentos = pagamentos;
    }

    public async Task<(CompraIngresso Compra, Pagamento? Pagamento)> ComprarAsync(string compradorId, string compradorNome, int eventoId, int quantidade)
    {
        if (quantidade is < 1 or > MaximoPorCompra) throw new RegraDeNegocioException(TipoErro.Invalido, $"Escolha de 1 a {MaximoPorCompra} ingressos.");

        var evento = await _eventos.ObterPorIdAsync(eventoId);
        if (evento is null || evento.Status != StatusEvento.Aprovado) throw new RegraDeNegocioException(TipoErro.NaoEncontrado, "Evento não encontrado.");
        if (evento.Data < Evento.HojeBrasil()) throw new RegraDeNegocioException(TipoErro.Conflito, "Este evento já aconteceu.");

        var compra = new CompraIngresso
        {
            EventoId = eventoId, CompradorId = compradorId, CompradorNome = compradorNome, Quantidade = quantidade,
            PrecoUnitario = evento.Preco, Total = evento.Preco * quantidade,
            Codigo = Convert.ToHexString(RandomNumberGenerator.GetBytes(5))
        };

        if (evento.Preco <= 0)
        {
            compra.Status = StatusIngresso.Confirmado;
            compra.ConfirmadoEm = DateTime.UtcNow;
            await _repositorio.AdicionarAsync(compra);
            await _repositorio.SalvarAlteracoesAsync();
            return (compra, null);
        }

        if (!_pagamentos.Habilitado) throw new RegraDeNegocioException(TipoErro.Indisponivel, "A venda de ingressos ainda não está ativada nesta plataforma.");

        await _repositorio.AdicionarAsync(compra);
        await _repositorio.SalvarAlteracoesAsync();
        try
        {
            var pagamento = await _pagamentos.CriarAsync(TipoPagamento.Ingresso, compra.Id, compradorId, compradorNome,
                compra.Total, $"Ingresso: {evento.Titulo} (x{quantidade})");
            return (compra, pagamento);
        }
        catch
        {
            compra.Status = StatusIngresso.Cancelado;
            await _repositorio.SalvarAlteracoesAsync();
            throw;
        }
    }

    public Task<IReadOnlyList<IngressoDoUsuario>> ListarDoUsuarioAsync(string usuarioId) => _repositorio.ListarDoUsuarioAsync(usuarioId);

    public async Task CancelarAsync(string usuarioId, int compraId)
    {
        var compra = await _repositorio.ObterAsync(compraId);
        if (compra is null || compra.CompradorId != usuarioId) throw new RegraDeNegocioException(TipoErro.NaoEncontrado, "Compra não encontrada.");
        if (compra.Status != StatusIngresso.AguardandoPagamento) throw new RegraDeNegocioException(TipoErro.Conflito, "Só é possível cancelar compras que ainda não foram pagas.");

        compra.Status = StatusIngresso.Cancelado;
        await _repositorio.SalvarAlteracoesAsync();
        await _pagamentos.CancelarPendenteDoAlvoAsync(TipoPagamento.Ingresso, compra.Id);
    }
}

/// <summary>Quando o pagamento do ingresso é confirmado, a compra passa a Confirmada.</summary>
public class IngressoPagamentoHandler : IPagamentoConfirmadoHandler
{
    private readonly IIngressoRepository _repositorio;

    public IngressoPagamentoHandler(IIngressoRepository repositorio) => _repositorio = repositorio;

    public TipoPagamento Tipo => TipoPagamento.Ingresso;

    public async Task AoConfirmarAsync(Pagamento pagamento, CancellationToken ct = default)
    {
        var compra = await _repositorio.ObterAsync(pagamento.AlvoId);
        if (compra is { Status: StatusIngresso.AguardandoPagamento })
        {
            compra.Status = StatusIngresso.Confirmado;
            compra.ConfirmadoEm = DateTime.UtcNow;
        }
    }
}
