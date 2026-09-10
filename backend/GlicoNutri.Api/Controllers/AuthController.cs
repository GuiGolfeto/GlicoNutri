using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// UC001 — Login e Autenticação. Rotas públicas, exceto a troca de senha, que
/// exige o usuário já autenticado.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>Autentica o usuário e devolve o token de acesso válido por 24 horas.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<LoginFalhaResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<LoginFalhaResponse>(StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login(LoginRequest pedido, CancellationToken ct)
    {
        var resultado = await auth.LoginAsync(pedido, ct);

        if (resultado.Sucesso)
            return Ok(resultado.Dados);

        // RN02 — a tela de login precisa informar quanto falta para o desbloqueio.
        var segundos = resultado.BloqueadoAte is { } ate
            ? (int?)Math.Max(0, Math.Ceiling((ate - DateTime.UtcNow).TotalSeconds))
            : null;

        var falha = new LoginFalhaResponse(
            resultado.Mensagem!, resultado.Bloqueado, resultado.BloqueadoAte, segundos);

        return resultado.Bloqueado
            ? StatusCode(StatusCodes.Status423Locked, falha)
            : Unauthorized(falha);
    }

    /// <summary>
    /// RN01 — login federado com conta Google. O cliente envia o ID token obtido
    /// no Google Identity Services; o acesso só é concedido se o e-mail
    /// corresponder a um usuário previamente cadastrado e ativo.
    /// </summary>
    [HttpPost("login-google")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<LoginFalhaResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginGoogle(LoginGoogleRequest pedido, CancellationToken ct)
    {
        var resultado = await auth.LoginGoogleAsync(pedido, ct);
        if (resultado.Sucesso) return Ok(resultado.Dados);

        var segundos = resultado.BloqueadoAte is { } ate
            ? (int?)Math.Max(0, Math.Ceiling((ate - DateTime.UtcNow).TotalSeconds))
            : null;

        var falha = new LoginFalhaResponse(
            resultado.Mensagem!, resultado.Bloqueado, resultado.BloqueadoAte, segundos);

        return resultado.Bloqueado
            ? StatusCode(StatusCodes.Status423Locked, falha)
            : Unauthorized(falha);
    }

    /// <summary>
    /// RN03 do UC001 — renova o token de quem está em uso ativo, antes que a
    /// validade de 24 horas expire e derrube a sessão no meio do atendimento.
    /// </summary>
    [HttpPost("renovar")]
    [Authorize]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Renovar(CancellationToken ct)
    {
        var usuarioId = User.ObterUsuarioId();
        if (usuarioId is null) return Unauthorized();

        var resultado = await auth.RenovarAsync(usuarioId.Value, ct);
        return resultado.Sucesso
            ? Ok(resultado.Dados)
            : Unauthorized(new { mensagem = resultado.Mensagem });
    }

    /// <summary>
    /// Dispara o e-mail de redefinição de senha. Responde 202 mesmo quando o e-mail
    /// não existe, para não revelar quais contas estão cadastradas.
    /// </summary>
    [HttpPost("recuperar-senha")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RecuperarSenha(RecuperarSenhaRequest pedido, CancellationToken ct)
    {
        await auth.RecuperarSenhaAsync(pedido, ct);
        return Accepted(new { mensagem = "Se houver conta ativa para este e-mail, o link de redefinição foi enviado." });
    }

    /// <summary>Redefine a senha a partir do link recebido por e-mail.</summary>
    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RedefinirSenha(RedefinirSenhaRequest pedido, CancellationToken ct)
    {
        var resultado = await auth.RedefinirSenhaAsync(pedido, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>
    /// Troca a senha do usuário autenticado. Única rota liberada enquanto a
    /// credencial ainda for a provisória (RN03).
    /// </summary>
    [HttpPost("alterar-senha")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaRequest pedido, CancellationToken ct)
    {
        var usuarioId = User.ObterUsuarioId();
        if (usuarioId is null) return Unauthorized();

        var resultado = await auth.AlterarSenhaAsync(usuarioId.Value, pedido, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>Dados do usuário autenticado, usados pelo front para montar o menu por perfil.</summary>
    [HttpGet("eu")]
    [Authorize]
    public IActionResult Eu() => Ok(new
    {
        usuarioId = User.ObterUsuarioId(),
        email = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value,
        perfil = User.FindFirst(ClaimsGlicoNutri.Perfil)?.Value,
        senhaProvisoria = User.FindFirst(ClaimsGlicoNutri.SenhaProvisoria)?.Value == "true",
    });
}
