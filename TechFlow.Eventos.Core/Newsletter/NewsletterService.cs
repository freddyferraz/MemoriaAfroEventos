using System.Net.Mail;
using System.Security.Cryptography;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Core.Newsletter;

/// <summary>Regras do módulo Newsletter: inscrição com confirmação, cancelamento e envio por preferência.</summary>
public class NewsletterService
{
    private static readonly TimeSpan IntervaloReenvio = TimeSpan.FromMinutes(10);

    private readonly IAssinanteRepository _assinantes;
    private readonly IEventoRepository _eventos;
    private readonly IEmailSender _email;
    private readonly NewsletterOptions _options;

    public NewsletterService(IAssinanteRepository assinantes, IEventoRepository eventos, IEmailSender email, NewsletterOptions options)
    {
        _assinantes = assinantes;
        _eventos = eventos;
        _email = email;
        _options = options;
    }

    /// <summary>
    /// Cria (ou atualiza, se ainda não confirmado) a inscrição e envia o e-mail de confirmação.
    /// Quem já está confirmado não recebe nada: evita usar o formulário para disparar e-mails a terceiros.
    /// </summary>
    public async Task AssinarAsync(string email, IEnumerable<int> categorias, IEnumerable<string> cidades, CancellationToken ct = default)
    {
        var endereco = NormalizarEmail(email) ?? throw new ArgumentException("E-mail inválido.");
        var cats = categorias.Distinct().Where(c => Enum.IsDefined(typeof(CategoriaEvento), c)).OrderBy(c => c).ToArray();
        var cids = cidades.Select(c => c.Trim()).Where(c => c.Length is > 0 and <= 120)
                          .Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();

        var assinante = await _assinantes.ObterPorEmailAsync(endereco);
        if (assinante is { Confirmado: true }) return;

        var agora = DateTime.UtcNow;
        if (assinante is null)
        {
            assinante = new Assinante
            {
                Email = endereco, Categorias = cats, Cidades = cids,
                Token = NovoToken(), DataAceite = agora
            };
            await _assinantes.AdicionarAsync(assinante);
        }
        else
        {
            if (assinante.ConfirmacaoEnviadaEm is { } ultimo && agora - ultimo < IntervaloReenvio) return;
            assinante.Categorias = cats;
            assinante.Cidades = cids;
            assinante.DataAceite = agora;
        }

        await _assinantes.SalvarAlteracoesAsync();
        await _email.EnviarAsync(EmailTemplates.Confirmacao(_options, assinante), ct);
        assinante.ConfirmacaoEnviadaEm = DateTime.UtcNow;
        await _assinantes.SalvarAlteracoesAsync();
    }

    public async Task<bool> ConfirmarAsync(string token)
    {
        var assinante = await _assinantes.ObterPorTokenAsync(token);
        if (assinante is null) return false;

        if (!assinante.Confirmado)
        {
            assinante.Confirmado = true;
            assinante.DataConfirmacao = DateTime.UtcNow;
            await _assinantes.SalvarAlteracoesAsync();
        }

        return true;
    }

    /// <summary>Descadastro: remove o assinante e o histórico de envios (os dados deixam de existir).</summary>
    public async Task<bool> CancelarAsync(string token)
    {
        var assinante = await _assinantes.ObterPorTokenAsync(token);
        if (assinante is null) return false;

        _assinantes.Remover(assinante);
        await _assinantes.SalvarAlteracoesAsync();
        return true;
    }

    /// <summary>Envia o evento aprovado aos assinantes confirmados cujas preferências combinam. Idempotente por assinante.</summary>
    public async Task<ResultadoEnvio> NotificarEventoAsync(int eventoId, CancellationToken ct = default)
    {
        var evento = await _eventos.ObterPorIdAsync(eventoId);
        if (evento is null || evento.Status != StatusEvento.Aprovado || evento.Data < Evento.HojeBrasil())
        {
            return new ResultadoEnvio(0, 0, 0, null);
        }

        var jaEnviados = await _assinantes.ListarIdsJaNotificadosAsync(eventoId);
        var destinatarios = (await _assinantes.ListarConfirmadosAsync())
            .Where(a => a.QuerReceber(evento) && !jaEnviados.Contains(a.Id))
            .ToList();

        int enviados = 0, falhas = 0;
        string? ultimoErro = null;
        foreach (var assinante in destinatarios)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await _email.EnviarAsync(EmailTemplates.NovoEvento(_options, assinante, evento), ct);
                await _assinantes.RegistrarEnvioAsync(eventoId, assinante.Id);
                enviados++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                falhas++;
                ultimoErro = ex.Message;
            }
        }

        return new ResultadoEnvio(destinatarios.Count, enviados, falhas, ultimoErro);
    }

    private static string? NormalizarEmail(string? email)
    {
        var e = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(e) || e.Length > 254) return null;
        return MailAddress.TryCreate(e, out var m) && m.Address == e && e.Contains('.', StringComparison.Ordinal) ? e : null;
    }

    private static string NovoToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
}
