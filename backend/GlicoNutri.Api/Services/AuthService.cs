using System.IdentityModel.Tokens.Jwt;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Repositories;
using GlicoNutri.Api.Security;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace GlicoNutri.Api.Services;

public record ResultadoLogin(
    bool Sucesso,
    LoginResponse? Dados = null,
    string? Mensagem = null,
    bool Bloqueado = false,
    DateTime? BloqueadoAte = null);

public record ResultadoOperacao(bool Sucesso, string? Mensagem = null);

public interface IAuthService
{
    Task<ResultadoLogin> LoginAsync(LoginRequest pedido, CancellationToken ct = default);

    /// <summary>RN01 — login federado com conta Google no sistema web.</summary>
    Task<ResultadoLogin> LoginGoogleAsync(LoginGoogleRequest pedido, CancellationToken ct = default);

    /// <summary>RN03 do UC001 — renova o token de quem está em uso ativo.</summary>
    Task<ResultadoLogin> RenovarAsync(long usuarioId, CancellationToken ct = default);
    Task RecuperarSenhaAsync(RecuperarSenhaRequest pedido, CancellationToken ct = default);
    Task<ResultadoOperacao> RedefinirSenhaAsync(RedefinirSenhaRequest pedido, CancellationToken ct = default);
    Task<ResultadoOperacao> AlterarSenhaAsync(long usuarioId, AlterarSenhaRequest pedido, CancellationToken ct = default);
}

/// <summary>
/// UC001 — Login e Autenticação. Concentra a validação de credenciais (RN32),
/// o bloqueio progressivo (RN02) e a troca obrigatória da senha provisória (RN03).
/// </summary>
public class AuthService(
    IUsuarioRepository usuarios,
    ISenhaService senhas,
    ITokenService tokens,
    IEmailService emails,
    IOptions<GoogleOptions> google,
    ILogger<AuthService> log) : IAuthService
{
    private readonly GoogleOptions _google = google.Value;

    /// <summary>RN02 — cinco tentativas inválidas consecutivas disparam o bloqueio.</summary>
    private const int TentativasAteBloqueio = 5;

    private const string CredencialInvalida = "E-mail ou senha inválidos.";

    /// <summary>
    /// RN02 — a espera cresce a cada bloqueio: 5 minutos, 30 minutos e, do terceiro
    /// em diante, 24 horas por bloqueio até que o usuário consiga acessar.
    /// </summary>
    private static TimeSpan EsperaDoNivel(int nivel) => nivel switch
    {
        1 => TimeSpan.FromMinutes(5),
        2 => TimeSpan.FromMinutes(30),
        _ => TimeSpan.FromHours(24),
    };

    public async Task<ResultadoLogin> LoginAsync(LoginRequest pedido, CancellationToken ct = default)
    {
        var email = pedido.Email.Trim().ToLowerInvariant();
        var usuario = await usuarios.BuscarPorEmailAsync(email, ct);

        // Usuário inexistente ou desativado (RN05) recebe a mesma resposta de
        // credencial inválida, para não revelar quais e-mails existem na base.
        if (usuario is null)
            return new ResultadoLogin(false, Mensagem: CredencialInvalida);

        var agora = DateTime.UtcNow;

        if (usuario.BloqueadoAte is { } bloqueadoAte)
        {
            if (bloqueadoAte > agora)
            {
                return new ResultadoLogin(
                    false,
                    Mensagem: "Conta temporariamente bloqueada por tentativas inválidas.",
                    Bloqueado: true,
                    BloqueadoAte: bloqueadoAte);
            }

            // Bloqueio expirou: libera o acesso, mas preserva o nível já atingido
            // para que o próximo bloqueio seja mais longo, conforme a RN02.
            usuario.BloqueadoAte = null;
            usuario.TentativasInvalidas = 0;
        }

        if (!senhas.Conferir(pedido.Senha, usuario.SenhaHash))
        {
            usuario.TentativasInvalidas++;

            if (usuario.TentativasInvalidas >= TentativasAteBloqueio)
            {
                usuario.NivelBloqueio++;
                usuario.BloqueadoAte = agora.Add(EsperaDoNivel(usuario.NivelBloqueio));
                usuario.TentativasInvalidas = 0;

                await usuarios.SalvarAsync(ct);
                log.LogWarning("Conta {UsuarioId} bloqueada até {Ate} (nível {Nivel}).",
                    usuario.Id, usuario.BloqueadoAte, usuario.NivelBloqueio);

                // UC001 A5 — o usuário é avisado do bloqueio por e-mail. Ele pode
                // não ter sido quem tentou entrar.
                await emails.EnviarContaBloqueadaAsync(
                    usuario.Email, usuario.Nome, usuario.BloqueadoAte.Value, ct);

                return new ResultadoLogin(
                    false,
                    Mensagem: "Conta bloqueada após cinco tentativas inválidas.",
                    Bloqueado: true,
                    BloqueadoAte: usuario.BloqueadoAte);
            }

            await usuarios.SalvarAsync(ct);
            return new ResultadoLogin(false, Mensagem: CredencialInvalida);
        }

        // Autenticação bem-sucedida: zera o ciclo de bloqueio por completo.
        usuario.TentativasInvalidas = 0;
        usuario.NivelBloqueio = 0;
        usuario.BloqueadoAte = null;
        usuario.UltimoAcesso = agora;
        await usuarios.SalvarAsync(ct);

        return Autenticado(usuario);
    }

    /// <summary>
    /// RN01 — autenticação federada. O login com Google não cria conta nem
    /// dispensa o vínculo com um perfil ativo: quem não foi cadastrado pelo
    /// Administrador recebe mensagem informativa e tem o acesso negado.
    /// </summary>
    public async Task<ResultadoLogin> LoginGoogleAsync(
        LoginGoogleRequest pedido, CancellationToken ct = default)
    {
        if (!_google.EstaConfigurado)
            return new ResultadoLogin(false, Mensagem: "Login com Google não está configurado neste ambiente.");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(pedido.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_google.ClientId!],
                });
        }
        catch (InvalidJwtException e)
        {
            log.LogWarning(e, "Token do Google recusado na validação.");
            return new ResultadoLogin(false, Mensagem: "Não foi possível validar sua conta Google.");
        }

        // Uma conta Google sem e-mail verificado não prova identidade.
        if (!payload.EmailVerified)
            return new ResultadoLogin(false,
                Mensagem: "A conta Google informada não tem o e-mail verificado.");

        var email = payload.Email.Trim().ToLowerInvariant();
        var usuario = await usuarios.BuscarPorEmailAsync(email, ct);

        if (usuario is null)
        {
            log.LogInformation("Login Google negado: {Email} não corresponde a usuário ativo.", email);
            return new ResultadoLogin(false, Mensagem:
                "Este e-mail não está cadastrado no GlicoNutri. " +
                "O acesso precisa ser criado por um administrador da ADJ.");
        }

        // O bloqueio da RN02 vale para qualquer forma de entrada: trocar de via
        // de autenticação não pode ser o atalho para escapar dele.
        if (usuario.BloqueadoAte is { } ate && ate > DateTime.UtcNow)
        {
            return new ResultadoLogin(false,
                Mensagem: "Conta temporariamente bloqueada por tentativas inválidas.",
                Bloqueado: true, BloqueadoAte: ate);
        }

        usuario.TentativasInvalidas = 0;
        usuario.NivelBloqueio = 0;
        usuario.BloqueadoAte = null;
        usuario.UltimoAcesso = DateTime.UtcNow;
        await usuarios.SalvarAsync(ct);

        return Autenticado(usuario);
    }

    /// <summary>
    /// RN03 do UC001 — o token é renovado em uso ativo. A renovação parte de uma
    /// sessão já autenticada e ainda válida: nada é emitido para token expirado,
    /// para conta desativada ou para quem ainda usa a senha provisória.
    /// </summary>
    public async Task<ResultadoLogin> RenovarAsync(long usuarioId, CancellationToken ct = default)
    {
        var usuario = await usuarios.BuscarPorIdAsync(usuarioId, ct);
        if (usuario is null)
            return new ResultadoLogin(false, Mensagem: "Sessão inválida.");

        usuario.UltimoAcesso = DateTime.UtcNow;
        await usuarios.SalvarAsync(ct);

        return Autenticado(usuario);
    }

    private ResultadoLogin Autenticado(Models.Usuario usuario)
    {
        var perfil = usuario.Perfil.Codigo;
        var (token, expiraEm) = tokens.GerarTokenAcesso(usuario, perfil);

        return new ResultadoLogin(true, new LoginResponse(
            token, expiraEm, usuario.Id, usuario.Nome, usuario.Email, perfil, usuario.SenhaProvisoria));
    }

    /// <summary>
    /// Envia o link de redefinição. Permanece disponível em qualquer nível de
    /// bloqueio, como alternativa imediata à espera (RN02, parágrafo final).
    /// Responde sempre da mesma forma, exista o e-mail ou não.
    /// </summary>
    public async Task RecuperarSenhaAsync(RecuperarSenhaRequest pedido, CancellationToken ct = default)
    {
        var email = pedido.Email.Trim().ToLowerInvariant();
        var usuario = await usuarios.BuscarPorEmailAsync(email, ct);

        if (usuario is null)
        {
            log.LogInformation("Recuperação de senha solicitada para e-mail sem conta ativa.");
            return;
        }

        var token = tokens.GerarTokenRecuperacao(usuario);
        await emails.EnviarRecuperacaoSenhaAsync(usuario.Email, usuario.Nome, token, ct);
    }

    public async Task<ResultadoOperacao> RedefinirSenhaAsync(
        RedefinirSenhaRequest pedido, CancellationToken ct = default)
    {
        var principal = tokens.ValidarTokenRecuperacao(pedido.Token);
        if (principal is null)
            return new ResultadoOperacao(false, "Link de recuperação inválido ou expirado.");

        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!long.TryParse(sub, out var usuarioId))
            return new ResultadoOperacao(false, "Link de recuperação inválido ou expirado.");

        var usuario = await usuarios.BuscarPorIdAsync(usuarioId, ct);
        if (usuario is null)
            return new ResultadoOperacao(false, "Link de recuperação inválido ou expirado.");

        // Uso único: a impressão digital embutida no token deixa de bater assim que
        // a senha é trocada, invalidando qualquer link antigo ainda em circulação.
        var impressaoDoToken = principal.FindFirst(ClaimsGlicoNutri.ImpressaoSenha)?.Value;
        if (impressaoDoToken != senhas.ImpressaoDigital(usuario.SenhaHash))
            return new ResultadoOperacao(false, "Este link já foi utilizado.");

        usuario.SenhaHash = senhas.Hash(pedido.NovaSenha);
        usuario.SenhaProvisoria = false;

        // Redefinir a senha também encerra o bloqueio em curso (RN02).
        usuario.TentativasInvalidas = 0;
        usuario.NivelBloqueio = 0;
        usuario.BloqueadoAte = null;

        await usuarios.SalvarAsync(ct);
        log.LogInformation("Senha redefinida para o usuário {UsuarioId}.", usuario.Id);

        return new ResultadoOperacao(true);
    }

    /// <summary>
    /// Troca de senha pelo próprio usuário autenticado. É a única operação liberada
    /// enquanto a credencial ainda for a provisória gerada pelo sistema (RN03).
    /// </summary>
    public async Task<ResultadoOperacao> AlterarSenhaAsync(
        long usuarioId, AlterarSenhaRequest pedido, CancellationToken ct = default)
    {
        var usuario = await usuarios.BuscarPorIdAsync(usuarioId, ct);
        if (usuario is null)
            return new ResultadoOperacao(false, "Usuário não encontrado.");

        if (!senhas.Conferir(pedido.SenhaAtual, usuario.SenhaHash))
            return new ResultadoOperacao(false, "Senha atual incorreta.");

        if (senhas.Conferir(pedido.NovaSenha, usuario.SenhaHash))
            return new ResultadoOperacao(false, "A nova senha deve ser diferente da atual.");

        usuario.SenhaHash = senhas.Hash(pedido.NovaSenha);
        usuario.SenhaProvisoria = false;
        await usuarios.SalvarAsync(ct);

        return new ResultadoOperacao(true);
    }
}
