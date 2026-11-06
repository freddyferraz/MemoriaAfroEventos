namespace TechFlow.Eventos.Core.Newsletter;

public interface IAssinanteRepository
{
    Task<Assinante?> ObterPorEmailAsync(string email);
    Task<Assinante?> ObterPorTokenAsync(string token);
    Task<IReadOnlyList<Assinante>> ListarConfirmadosAsync();
    Task<HashSet<int>> ListarIdsJaNotificadosAsync(int eventoId);
    Task AdicionarAsync(Assinante assinante);
    void Remover(Assinante assinante);
    Task RegistrarEnvioAsync(int eventoId, int assinanteId);
    Task SalvarAlteracoesAsync();
}

/// <summary>Envio de e-mail. Implementações: Brevo (API HTTP) e log (desenvolvimento).</summary>
public interface IEmailSender
{
    Task EnviarAsync(EmailMensagem mensagem, CancellationToken ct = default);
}

/// <summary>Fila interna: o módulo de Eventos não espera o envio dos e-mails terminar.</summary>
public interface INewsletterDispatcher
{
    void EnfileirarEvento(int eventoId);
}
