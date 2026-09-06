using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Text.Json;

namespace GlicoNutri.Api.Security;

/// <summary>
/// RN03 — quem ainda usa a senha provisória gerada pelo sistema só pode executar a
/// própria troca de senha. Qualquer outra rota autenticada é recusada até que a
/// credencial definitiva seja cadastrada.
/// </summary>
public class SenhaProvisoriaMiddleware(RequestDelegate proximo)
{
    /// <summary>Rotas liberadas mesmo com senha provisória.</summary>
    private static readonly string[] RotasLiberadas =
    [
        "/api/auth/alterar-senha",
        "/api/auth/logout",
    ];

    public async Task InvokeAsync(HttpContext contexto)
    {
        var usuario = contexto.User;

        if (usuario.Identity?.IsAuthenticated == true &&
            usuario.FindFirst(ClaimsGlicoNutri.SenhaProvisoria)?.Value == "true")
        {
            var caminho = contexto.Request.Path.Value ?? string.Empty;
            var liberada = RotasLiberadas.Any(r =>
                caminho.StartsWith(r, StringComparison.OrdinalIgnoreCase));

            if (!liberada)
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                contexto.Response.ContentType = MediaTypeNames.Application.Json;
                await contexto.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    mensagem = "É necessário definir uma nova senha antes de usar o sistema.",
                    trocaDeSenhaObrigatoria = true,
                }));
                return;
            }
        }

        await proximo(contexto);
    }
}

public static class SenhaProvisoriaMiddlewareExtensions
{
    public static IApplicationBuilder UseSenhaProvisoria(this IApplicationBuilder app) =>
        app.UseMiddleware<SenhaProvisoriaMiddleware>();
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id do usuário autenticado, extraído da claim "sub" do token.</summary>
    public static long? ObterUsuarioId(this System.Security.Claims.ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(sub, out var id) ? id : null;
    }
}
