using System.Globalization;
using System.Net;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Core.Newsletter;

/// <summary>Textos e HTML dos e-mails. HTML simples com estilos inline (compatível com clientes de e-mail).</summary>
public static class EmailTemplates
{
    private static readonly string[] Dias = { "domingo", "segunda-feira", "terça-feira", "quarta-feira", "quinta-feira", "sexta-feira", "sábado" };
    private static readonly string[] Meses = { "janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro" };

    public static EmailMensagem Confirmacao(NewsletterOptions o, Assinante a)
    {
        var link = $"{o.BaseUrlLimpa}/newsletter.html?confirmar={a.Token}";
        var html = Layout(o, "Confirme sua inscrição",
            $"<p>Recebemos um pedido para enviar a você, por e-mail, os novos eventos da agenda cultural conforme suas preferências.</p>" +
            $"<p>{Botao(link, "Confirmar inscrição")}</p>" +
            "<p style=\"color:#586654;font-size:14px\">Se não foi você, ignore esta mensagem: nada será enviado sem a confirmação.</p>");
        var texto = $"Confirme sua inscrição na newsletter do {o.NomeRemetente}: {link}\nSe não foi você, ignore esta mensagem.";
        return new EmailMensagem(a.Email, "Confirme sua inscrição na newsletter de eventos", html, texto);
    }

    public static EmailMensagem NovoEvento(NewsletterOptions o, Assinante a, Evento e)
    {
        var detalhe = $"{o.BaseUrlLimpa}/evento.html?id={e.Id}";
        var cancelar = $"{o.BaseUrlLimpa}/newsletter.html?cancelar={a.Token}";
        var hora = e.Horario.ToString("HH:mm", CultureInfo.InvariantCulture);
        var quando = $"{Dia(e.Data)}, às {hora}";
        var local = $"{e.Endereco}, {e.Cidade}";
        var preco = e.Gratuito ? "Gratuito" : "R$ " + e.Preco.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

        var foto = e.TemFoto
            ? $"<img src=\"{o.BaseUrlLimpa}/api/eventos/{e.Id}/foto\" alt=\"Foto do evento\" style=\"max-width:100%;height:auto;border-radius:8px;margin:0 0 16px\" />"
            : string.Empty;

        var corpo =
            foto +
            $"<h2 style=\"margin:0 0 8px;font-size:22px\">{H(e.Titulo)}</h2>" +
            $"<p style=\"margin:0 0 12px;color:#586654\">{H(e.Categoria.Nome())}</p>" +
            $"<p style=\"margin:0 0 4px\"><strong>Quando:</strong> {H(quando)}</p>" +
            $"<p style=\"margin:0 0 4px\"><strong>Onde:</strong> {H(local)}</p>" +
            $"<p style=\"margin:0 0 16px\"><strong>Preço:</strong> {H(preco)}</p>" +
            $"<p>{Botao(detalhe, "Ver detalhes do evento")}</p>" +
            $"<p style=\"color:#586654;font-size:13px;margin-top:28px\">Você recebe este e-mail porque se inscreveu na newsletter de eventos com estas preferências. " +
            $"<a href=\"{cancelar}\" style=\"color:#586654\">Cancelar inscrição</a></p>";

        var texto = $"{e.Titulo}\n{e.Categoria.Nome()}\nQuando: {quando}\nOnde: {local}\nPreço: {preco}\nDetalhes: {detalhe}\n\nCancelar inscrição: {cancelar}";
        return new EmailMensagem(a.Email, $"Novo evento: {e.Titulo} ({e.Data:dd/MM})", Layout(o, null, corpo), texto);
    }

    private static string Layout(NewsletterOptions o, string? titulo, string conteudo) =>
        "<!DOCTYPE html><html lang=\"pt-br\"><body style=\"margin:0;background:#f3f6f1;font-family:Arial,Helvetica,sans-serif;color:#1d2a1a\">" +
        "<div style=\"max-width:560px;margin:0 auto;padding:24px 16px\">" +
        $"<p style=\"font-size:20px;font-weight:bold;margin:0 0 16px\"><span style=\"color:#DA7E42\">TECH</span><span style=\"color:#309CC9\">FLOW</span> Eventos</p>" +
        "<div style=\"background:#ffffff;border-radius:12px;padding:24px;line-height:1.5\">" +
        (titulo is null ? string.Empty : $"<h2 style=\"margin:0 0 12px\">{H(titulo)}</h2>") +
        conteudo + "</div></div></body></html>";

    private static string Botao(string url, string texto) =>
        $"<a href=\"{url}\" style=\"display:inline-block;background:#3D5C30;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:8px;font-weight:bold\">{H(texto)}</a>";

    private static string Dia(DateOnly d) => $"{Dias[(int)d.DayOfWeek]}, {d.Day:00} de {Meses[d.Month - 1]}";

    private static string H(string s) => WebUtility.HtmlEncode(s);
}
