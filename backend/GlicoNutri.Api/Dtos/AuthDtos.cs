using System.ComponentModel.DataAnnotations;

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

public record RecuperarSenhaRequest([Required, EmailAddress] string Email);

public record RedefinirSenhaRequest(
    [Required] string Token,
    [Required, MinLength(8)] string NovaSenha);

public record AlterarSenhaRequest(
    [Required] string SenhaAtual,
    [Required, MinLength(8)] string NovaSenha);
