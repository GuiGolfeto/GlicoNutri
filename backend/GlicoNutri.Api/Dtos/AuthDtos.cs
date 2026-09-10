using System.ComponentModel.DataAnnotations;
using GlicoNutri.Api.Security;

namespace GlicoNutri.Api.Dtos;

// Em records posicionais os atributos de validação precisam ficar no parâmetro do
// construtor primário: com o alvo "property:" o MVC os ignora e lança em tempo
// de execução ao validar o modelo.

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Senha);

/// <param name="SenhaProvisoria">RN03 — enquanto verdadeiro, o cliente deve forçar a troca de senha.</param>
public record LoginResponse(
    string Token,
    DateTime ExpiraEm,
    long UsuarioId,
    string Nome,
    string Email,
    string Perfil,
    bool SenhaProvisoria);

/// <summary>
/// Resposta de credencial inválida ou conta bloqueada. A RN02 exige informar ao
/// usuário quanto falta para o desbloqueio automático.
/// </summary>
public record LoginFalhaResponse(
    string Mensagem,
    bool Bloqueado,
    DateTime? BloqueadoAte,
    int? SegundosRestantes);

/// <summary>
/// RN01 — login federado com Google no sistema web. O cliente obtém o ID token
/// pelo Google Identity Services e o envia aqui; a API valida a assinatura e só
/// emite credencial se o e-mail corresponder a um usuário já cadastrado.
/// </summary>
public record LoginGoogleRequest([Required] string IdToken);

public record RecuperarSenhaRequest([Required, EmailAddress] string Email);

public record RedefinirSenhaRequest(
    [Required] string Token,
    [SenhaForte] string NovaSenha);

public record AlterarSenhaRequest(
    [Required] string SenhaAtual,
    [SenhaForte] string NovaSenha);
