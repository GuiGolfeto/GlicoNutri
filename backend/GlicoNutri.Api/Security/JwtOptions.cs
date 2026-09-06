using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Security;

public class JwtOptions
{
    public const string Secao = "Jwt";

    [Required] public string Issuer { get; set; } = null!;
    [Required] public string Audience { get; set; } = null!;

    /// <summary>Mínimo de 32 bytes, exigido pelo HMAC-SHA256.</summary>
    [Required, MinLength(32)] public string SigningKey { get; set; } = null!;

    /// <summary>UC001, passo 7 — token com validade de 24 horas.</summary>
    [Range(1, 168)] public int ExpiresHours { get; set; } = 24;
}

/// <summary>Nomes de claim próprios do GlicoNutri.</summary>
public static class ClaimsGlicoNutri
{
    public const string Perfil = "perfil";
    public const string SenhaProvisoria = "senha_provisoria";
    public const string Proposito = "proposito";
    public const string ImpressaoSenha = "isenha";
}

/// <summary>Nomes das políticas de autorização por perfil (RN04).</summary>
public static class Politicas
{
    public const string Paciente = "Paciente";
    public const string Nutricionista = "Nutricionista";
    public const string Administrador = "Administrador";
}
