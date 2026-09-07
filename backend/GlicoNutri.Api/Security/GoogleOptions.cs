namespace GlicoNutri.Api.Security;

/// <summary>
/// RN01 — autenticação federada com conta Google no sistema web. Sem ClientId
/// configurado o recurso fica desligado e a rota responde 501, em vez de falhar
/// de forma obscura.
/// </summary>
public class GoogleOptions
{
    public const string Secao = "Google";

    public string? ClientId { get; set; }

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(ClientId);
}
