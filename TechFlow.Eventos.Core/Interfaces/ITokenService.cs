namespace TechFlow.Eventos.Core.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiraEm) GerarToken(string usuarioId, string email, string nomeCompleto, IEnumerable<string> roles);
}
