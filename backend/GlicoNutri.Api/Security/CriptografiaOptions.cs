using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Security;

/// <summary>
/// RNF01 — criptografia por coluna dos dados sensíveis do paciente (CPF,
/// telefone, observações clínicas, medicação em uso) e do nutricionista
/// (telefone). Chaves em base64, geradas uma vez e guardadas só no
/// user-secrets — nunca no repositório.
/// </summary>
public class CriptografiaOptions
{
    public const string Secao = "Criptografia";

    /// <summary>Chave AES-256 (32 bytes em base64), usada para cifrar e decifrar.</summary>
    [Required] public string Chave { get; set; } = null!;

    /// <summary>
    /// Chave HMAC-SHA256, separada da de cifrar por boa prática (cada algoritmo
    /// com sua própria chave). Usada só para o hash de busca do CPF — a cifra em
    /// si usa nonce aleatório e por isso não dá para comparar no banco.
    /// </summary>
    [Required] public string ChaveHash { get; set; } = null!;
}
