using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GlicoNutri.Api.Data;
using GlicoNutri.Api.Models;
using GlicoNutri.Api.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GlicoNutri.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiraEm) GerarTokenAcesso(Usuario usuario, string perfilCodigo);
    string GerarTokenRecuperacao(Usuario usuario);
    ClaimsPrincipal? ValidarTokenRecuperacao(string token);
}

public class TokenService(IOptions<JwtOptions> opcoes, ISenhaService senhaService) : ITokenService
{
    private readonly JwtOptions _opcoes = opcoes.Value;

    /// <summary>Janela curta para o link de recuperação de senha.</summary>
    private static readonly TimeSpan ValidadeRecuperacao = TimeSpan.FromHours(1);

    private SymmetricSecurityKey Chave =>
        new(Encoding.UTF8.GetBytes(_opcoes.SigningKey));

    public (string Token, DateTime ExpiraEm) GerarTokenAcesso(Usuario usuario, string perfilCodigo)
    {
        var expiraEm = DateTime.UtcNow.AddHours(_opcoes.ExpiresHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimsGlicoNutri.Perfil, perfilCodigo),
            new(ClaimsGlicoNutri.SenhaProvisoria, usuario.SenhaProvisoria.ToString().ToLowerInvariant()),
        };

        // RN04 — o Administrador herda as permissões do Nutricionista. Emitir as
        // duas roles resolve a herança na própria credencial: um endpoint marcado
        // para Nutricionista passa a aceitar o Administrador sem regra extra.
        claims.Add(new Claim(ClaimTypes.Role, perfilCodigo));
        if (perfilCodigo == Codigos.Perfil.Administrador)
            claims.Add(new Claim(ClaimTypes.Role, Codigos.Perfil.Nutricionista));

        var token = new JwtSecurityToken(
            issuer: _opcoes.Issuer,
            audience: _opcoes.Audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: new SigningCredentials(Chave, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }

    /// <summary>
    /// Token de recuperação de senha. Carrega a impressão digital do hash atual:
    /// como redefinir a senha troca o hash, o token queima sozinho após o uso,
    /// dispensando uma tabela de tokens que o DER V2.0 não prevê.
    /// </summary>
    public string GerarTokenRecuperacao(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimsGlicoNutri.Proposito, "recuperacao"),
            new(ClaimsGlicoNutri.ImpressaoSenha, senhaService.ImpressaoDigital(usuario.SenhaHash)),
        };

        var token = new JwtSecurityToken(
            issuer: _opcoes.Issuer,
            audience: _opcoes.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(ValidadeRecuperacao),
            signingCredentials: new SigningCredentials(Chave, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidarTokenRecuperacao(string token)
    {
        var parametros = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _opcoes.Issuer,
            ValidateAudience = true,
            ValidAudience = _opcoes.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = Chave,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        try
        {
            // MapInboundClaims desligado pelo mesmo motivo do JwtBearer: com o
            // remapeamento padrão, "sub" viraria ClaimTypes.NameIdentifier e a
            // leitura do id do usuário falharia silenciosamente.
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(token, parametros, out _);

            return principal.FindFirst(ClaimsGlicoNutri.Proposito)?.Value == "recuperacao"
                ? principal
                : null;
        }
        catch (Exception e) when (e is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }
}
