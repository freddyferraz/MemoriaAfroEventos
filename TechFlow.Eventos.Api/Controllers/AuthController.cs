using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Api.DTOs;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Interfaces;
using TechFlow.Eventos.Core.Newsletter;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private const string Moderadores = $"{DbSeeder.RoleAdmin},{DbSeeder.RoleModerador}";

    private readonly ITokenService _tokens;
    private readonly ILogger<AuthController> _log;
    private readonly IEmailSender? _email;
    private readonly NewsletterOptions? _emailOptions;

    // IEmailSender e NewsletterOptions são opcionais: sem o módulo Newsletter, o aviso de aprovação é só dispensado.
    public AuthController(UserManager<ApplicationUser> users, ITokenService tokens, ILogger<AuthController> log,
        IEmailSender? email = null, NewsletterOptions? emailOptions = null)
    {
        _users = users;
        _tokens = tokens;
        _log = log;
        _email = email;
        _emailOptions = emailOptions;
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

        // A senha já foi conferida: só agora informamos a situação do cadastro.
        if (user.Status == StatusCadastro.Pendente)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "Seu cadastro está aguardando a aprovação de um moderador." });
        }

        if (user.Status == StatusCadastro.Rejeitado)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "Seu cadastro não foi aprovado." });
        }

        var roles = await _users.GetRolesAsync(user);
        var (token, expiraEm) = _tokens.GerarToken(user.Id, user.Email!, user.NomeCompleto, roles);
        return Ok(new AuthResponseDto
        {
            Token = token, ExpiraEm = expiraEm, Email = user.Email!,
            NomeCompleto = user.NomeCompleto, Roles = roles
        });
    }

    /// <summary>Criar conta. A conta nasce Pendente: o login só é liberado depois que um moderador aprova.</summary>
    [HttpPost("registrar")]
    [AllowAnonymous]
    [EnableRateLimiting("cadastro")]
    public async Task<IActionResult> Registrar(RegistrarDto dto)
    {
        if (!dto.AceiteTermos)
        {
            return BadRequest(new { erro = "É necessário concordar com o uso dos dados da conta para se cadastrar." });
        }

        if (await _users.FindByEmailAsync(dto.Email.Trim()) is not null)
        {
            return Conflict(new { erro = "Já existe um cadastro com este e-mail." });
        }

        var email = dto.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email, Email = email, NomeCompleto = dto.NomeCompleto.Trim(),
            Status = StatusCadastro.Pendente, DataCadastro = DateTime.UtcNow
        };

        var r = await _users.CreateAsync(user, dto.Senha);
        if (!r.Succeeded)
        {
            return BadRequest(new { erro = string.Join(" ", r.Errors.Select(e => e.Description)) });
        }

        await _users.AddToRoleAsync(user, DbSeeder.RoleCliente);
        return Accepted(new { mensagem = "Cadastro recebido! Um moderador vai analisá-lo e você poderá entrar assim que for aprovado." });
    }

    /// <summary>Cadastros aguardando aprovação. Restrito a Admin/Moderador.</summary>
    [HttpGet("cadastros/pendentes")]
    [Authorize(Roles = Moderadores)]
    public async Task<IActionResult> CadastrosPendentes() =>
        Ok(await _users.Users.AsNoTracking()
            .Where(u => u.Status == StatusCadastro.Pendente)
            .OrderBy(u => u.DataCadastro)
            .Select(u => new { u.Id, u.NomeCompleto, u.Email, u.DataCadastro })
            .ToListAsync());

    /// <summary>Aprova ou rejeita um cadastro pendente. Só cadastros pendentes podem ser analisados.</summary>
    [HttpPatch("cadastros/{id}/moderar")]
    [Authorize(Roles = Moderadores)]
    public async Task<IActionResult> ModerarCadastro(string id, ModerarCadastroDto dto)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (user.Status != StatusCadastro.Pendente)
        {
            return Conflict(new { erro = "Este cadastro já foi analisado." });
        }

        user.Status = dto.Aprovar ? StatusCadastro.Aprovado : StatusCadastro.Rejeitado;
        var r = await _users.UpdateAsync(user);
        if (!r.Succeeded) return BadRequest(new { erro = string.Join(" ", r.Errors.Select(e => e.Description)) });

        if (dto.Aprovar) await AvisarAprovacaoAsync(user);
        return NoContent();
    }

    private async Task AvisarAprovacaoAsync(ApplicationUser user)
    {
        if (_email is null || _emailOptions is null || string.IsNullOrEmpty(user.Email)) return;
        try
        {
            await _email.EnviarAsync(EmailTemplates.CadastroAprovado(_emailOptions, user.NomeCompleto, user.Email));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Cadastro aprovado, mas o e-mail de aviso não pôde ser enviado.");
        }
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
