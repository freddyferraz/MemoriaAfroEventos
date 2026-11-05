using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TechFlow.Eventos.Api.DTOs;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;

    public AuthController(UserManager<ApplicationUser> users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user is null || !await _users.CheckPasswordAsync(user, dto.Senha))
        {
            return Unauthorized(new { erro = "E-mail ou senha inválidos." });
        }

        var roles = await _users.GetRolesAsync(user);
        var (token, expiraEm) = _tokens.GerarToken(user.Id, user.Email!, user.NomeCompleto, roles);
        return Ok(new AuthResponseDto
        {
            Token = token, ExpiraEm = expiraEm, Email = user.Email!,
            NomeCompleto = user.NomeCompleto, Roles = roles
        });
    }

    /// <summary>Cria uma conta de moderador. Restrito a administradores.</summary>
    [HttpPost("moderadores")]
    [Authorize(Roles = DbSeeder.RoleAdmin)]
    public async Task<IActionResult> CriarModerador(NovoModeradorDto dto)
    {
        if (await _users.FindByEmailAsync(dto.Email) is not null)
        {
            return Conflict(new { erro = "Já existe uma conta com este e-mail." });
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email, Email = dto.Email, EmailConfirmed = true, NomeCompleto = dto.NomeCompleto
        };
        var r = await _users.CreateAsync(user, dto.Senha);
        if (!r.Succeeded) return BadRequest(r.Errors.Select(e => e.Description));

        await _users.AddToRoleAsync(user, DbSeeder.RoleModerador);
        return StatusCode(StatusCodes.Status201Created, new { user.Id, user.Email });
    }
}
