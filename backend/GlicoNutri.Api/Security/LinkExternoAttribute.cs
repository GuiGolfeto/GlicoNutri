using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Security;

/// <summary>
/// RF09.2 — valida o link de mídia do conteúdo educativo.
///
/// A validação existe por segurança, não por estética: o endereço é renderizado
/// na tela do paciente, e aceitar qualquer esquema abriria espaço para
/// javascript: e data: entrarem como "conteúdo educativo" publicado.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class LinkExternoAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? valor, ValidationContext contexto)
    {
        if (valor is not string texto || string.IsNullOrWhiteSpace(texto))
            return ValidationResult.Success;

        if (!Uri.TryCreate(texto.Trim(), UriKind.Absolute, out var uri))
            return new ValidationResult("Informe um endereço completo, começando com https://.");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return new ValidationResult("O endereço precisa começar com http:// ou https://.");

        return ValidationResult.Success;
    }
}
