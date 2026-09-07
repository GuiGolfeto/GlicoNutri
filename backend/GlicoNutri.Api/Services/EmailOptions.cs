using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Services;

public class EmailOptions
{
    public const string Secao = "Email";

    /// <summary>
    /// Enquanto falso, o sistema apenas registra as mensagens no log. Ligar exige
    /// as credenciais SMTP, que ficam no user-secrets, nunca no repositório.
    /// </summary>
    public bool Habilitado { get; set; }

    public string? Host { get; set; }
    public int Porta { get; set; } = 587;
    public string? Usuario { get; set; }
    public string? Senha { get; set; }

    [EmailAddress] public string? Remetente { get; set; }
    public string RemetenteNome { get; set; } = "GlicoNutri";

    /// <summary>STARTTLS na 587; SSL direto na 465.</summary>
    public bool UsarSslDireto { get; set; }

    /// <summary>Base do sistema web, usada para montar o link de primeiro acesso.</summary>
    public string UrlWeb { get; set; } = "http://localhost:5173";

    public bool EstaConfigurado =>
        Habilitado
        && !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Remetente);
}
