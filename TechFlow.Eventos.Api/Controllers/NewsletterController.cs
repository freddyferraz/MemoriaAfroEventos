using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TechFlow.Eventos.Api.DTOs;
using TechFlow.Eventos.Core.Newsletter;

namespace TechFlow.Eventos.Api.Controllers;

/// <summary>Módulo Newsletter: inscrição com confirmação por e-mail (double opt-in) e cancelamento.</summary>
[ApiController]
[Route("api/[controller]")]
public class NewsletterController : ControllerBase
{
    private readonly NewsletterService _service;
    private readonly ILogger<NewsletterController> _log;

    public NewsletterController(NewsletterService service, ILogger<NewsletterController> log)
    {
        _service = service;
        _log = log;
    }

    /// <summary>
    /// Pede a inscrição. A resposta é sempre a mesma para e-mails novos, pendentes ou já confirmados,
    /// para não revelar quem está inscrito. Limitado por IP (política "newsletter").
    /// </summary>
    [HttpPost("assinar")]
    [AllowAnonymous]
    [EnableRateLimiting("newsletter")]
    public async Task<IActionResult> Assinar(AssinarNewsletterDto dto, CancellationToken ct)
    {
        if (!dto.AceiteLgpd)
        {
            return BadRequest(new { erro = "É necessário concordar em receber os e-mails para se inscrever." });
        }

        try
        {
            await _service.AssinarAsync(dto.Email, dto.Categorias ?? Array.Empty<int>(), dto.Cidades ?? Array.Empty<string>(), ct);
        }
        catch (ArgumentException)
        {
            return BadRequest(new { erro = "Informe um e-mail válido." });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "Falha ao enviar o e-mail de confirmação da newsletter.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { erro = "Não foi possível enviar o e-mail agora. Tente novamente em alguns minutos." });
        }

        return Accepted(new { mensagem = "Enviamos um e-mail com o link para confirmar sua inscrição. Se não encontrar, olhe a caixa de spam." });
    }

    [HttpPost("confirmar")]
    [AllowAnonymous]
    public async Task<IActionResult> Confirmar(TokenNewsletterDto dto) =>
        await _service.ConfirmarAsync(dto.Token)
            ? Ok(new { mensagem = "Inscrição confirmada. Você vai receber os novos eventos por e-mail." })
            : NotFound(new { erro = "Link inválido ou já utilizado." });

    [HttpPost("cancelar")]
    [AllowAnonymous]
    public async Task<IActionResult> Cancelar(TokenNewsletterDto dto) =>
        await _service.CancelarAsync(dto.Token)
            ? Ok(new { mensagem = "Inscrição cancelada. Seus dados foram removidos." })
            : NotFound(new { erro = "Link inválido ou inscrição já cancelada." });
}
