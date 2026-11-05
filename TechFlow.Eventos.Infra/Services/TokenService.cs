using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TechFlow.Eventos.Core.Interfaces;

namespace TechFlow.Eventos.Infra.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration) => _configuration = configuration;

    public (string Token, DateTime ExpiraEm) GerarToken(string usuarioId, string email, string nomeCompleto, IEnumerable<string> roles)
    {
        var jwt = _configuration.GetSection("Jwt");
        var chave = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurada.");
        var minutos = int.TryParse(jwt["ExpiresInMinutes"], out var m) ? m : 120;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioId),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, usuarioId),
            new("nomeCompleto", nomeCompleto)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var expiraEm = DateTime.UtcNow.AddMinutes(minutos);
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expiraEm,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)), SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
