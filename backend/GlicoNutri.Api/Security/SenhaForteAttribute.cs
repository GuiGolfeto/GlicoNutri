using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Security;

/// <summary>
/// RN02 do UC001 — a senha deve ter no mínimo 8 caracteres, incluindo letras e
/// números. O tamanho sozinho aceitaria "12345678", que é o que essa regra
/// existe para barrar.
/// </summary>
public sealed class SenhaForteAttribute : ValidationAttribute
{
    private const int TamanhoMinimo = 8;

    protected override ValidationResult? IsValid(object? valor, ValidationContext contexto)
    {
        if (valor is not string senha || string.IsNullOrEmpty(senha))
            return new ValidationResult("Informe a nova senha.");

        if (senha.Length < TamanhoMinimo)
            return new ValidationResult($"A senha deve ter no mínimo {TamanhoMinimo} caracteres.");

        if (!senha.Any(char.IsLetter))
            return new ValidationResult("A senha deve conter ao menos uma letra.");

        if (!senha.Any(char.IsDigit))
            return new ValidationResult("A senha deve conter ao menos um número.");

        return ValidationResult.Success;
    }
}
