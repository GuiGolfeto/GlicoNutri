namespace GlicoNutri.Api.Services;

public interface IEmailService
{
    Task EnviarRecuperacaoSenhaAsync(string email, string nome, string token, CancellationToken ct = default);
    Task EnviarBoasVindasAsync(string email, string nome, string senhaProvisoria, CancellationToken ct = default);

    /// <summary>UC001 A5 — o usuário é avisado quando a conta é bloqueada.</summary>
    Task EnviarContaBloqueadaAsync(
        string email, string nome, DateTime bloqueadaAte, CancellationToken ct = default);
}

/// <summary>
/// Implementação de desenvolvimento: registra a mensagem no log em vez de enviar.
/// O envio real (RF10.2 e RF10.3) entra na Fase 1, trocando só esta classe.
/// </summary>
public class EmailServiceLog(ILogger<EmailServiceLog> log) : IEmailService
{
    public Task EnviarRecuperacaoSenhaAsync(string email, string nome, string token, CancellationToken ct = default)
    {
        log.LogInformation("[E-MAIL] Recuperação de senha para {Nome} <{Email}> — token: {Token}",
            nome, email, token);
        return Task.CompletedTask;
    }

    public Task EnviarBoasVindasAsync(string email, string nome, string senhaProvisoria, CancellationToken ct = default)
    {
        log.LogInformation("[E-MAIL] Boas-vindas a {Nome} <{Email}> — senha provisória: {Senha}",
            nome, email, senhaProvisoria);
        return Task.CompletedTask;
    }

    public Task EnviarContaBloqueadaAsync(
        string email, string nome, DateTime bloqueadaAte, CancellationToken ct = default)
    {
        log.LogWarning("[E-MAIL] Conta de {Nome} <{Email}> bloqueada até {Ate}.",
            nome, email, bloqueadaAte);
        return Task.CompletedTask;
    }
}
