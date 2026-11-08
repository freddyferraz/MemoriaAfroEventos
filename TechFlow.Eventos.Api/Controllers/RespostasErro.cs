using Microsoft.AspNetCore.Mvc;
using TechFlow.Eventos.Core.Compartilhado;

namespace TechFlow.Eventos.Api.Controllers;

internal static class RespostasErro
{
    /// <summary>Traduz uma violação de regra de negócio no código HTTP correspondente.</summary>
    public static IActionResult Responder(this ControllerBase c, RegraDeNegocioException ex) => ex.Tipo switch
    {
        TipoErro.NaoEncontrado => c.NotFound(new { erro = ex.Message }),
        TipoErro.Conflito => c.Conflict(new { erro = ex.Message }),
        TipoErro.NaoPermitido => c.StatusCode(StatusCodes.Status403Forbidden, new { erro = ex.Message }),
        TipoErro.Indisponivel => c.StatusCode(StatusCodes.Status503ServiceUnavailable, new { erro = ex.Message }),
        _ => c.BadRequest(new { erro = ex.Message })
    };
}
