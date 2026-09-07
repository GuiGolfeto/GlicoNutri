using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GlicoNutri.Api.Services;

/// <summary>
/// Envio real por SMTP, usado quando a seção Email está configurada.
/// RF10.2 e RF10.3 — o paciente e o nutricionista recebem as credenciais de
/// primeiro acesso por e-mail.
/// </summary>
public class EmailServiceSmtp : IEmailService
{
    private readonly EmailOptions _opcoes;
    private readonly ILogger<EmailServiceSmtp> _log;

    // Host e remetente são exigidos por EstaConfigurado, que é o que decide o
    // registro desta implementação. A checagem no construtor torna a invariante
    // explícita e faz a falta de configuração aparecer na inicialização, e não
    // num NullReferenceException no primeiro cadastro.
    private readonly string _host;
    private readonly string _remetente;

    public EmailServiceSmtp(IOptions<EmailOptions> opcoes, ILogger<EmailServiceSmtp> log)
    {
        _opcoes = opcoes.Value;
        _log = log;

        _host = _opcoes.Host
            ?? throw new InvalidOperationException("Email:Host não configurado.");
        _remetente = _opcoes.Remetente
            ?? throw new InvalidOperationException("Email:Remetente não configurado.");
    }

    public Task EnviarBoasVindasAsync(
        string email, string nome, string senhaProvisoria, CancellationToken ct = default)
    {
        var corpo = $"""
            <p>Olá, {Escapar(nome)}.</p>
            <p>Sua conta no <strong>GlicoNutri</strong> foi criada. Use as credenciais
            abaixo no primeiro acesso:</p>
            <p>
              E-mail: <strong>{Escapar(email)}</strong><br />
              Senha provisória: <strong>{Escapar(senhaProvisoria)}</strong>
            </p>
            <p>Por segurança, o sistema vai pedir que você defina uma senha
            definitiva antes de liberar o acesso.</p>
            <p><a href="{_opcoes.UrlWeb}/login">Acessar o GlicoNutri</a></p>
            """;

        return EnviarAsync(email, nome, "Suas credenciais de acesso ao GlicoNutri", corpo, ct);
    }

    public Task EnviarRecuperacaoSenhaAsync(
        string email, string nome, string token, CancellationToken ct = default)
    {
        var link = $"{_opcoes.UrlWeb}/redefinir-senha?token={Uri.EscapeDataString(token)}";

        var corpo = $"""
            <p>Olá, {Escapar(nome)}.</p>
            <p>Recebemos um pedido de redefinição de senha da sua conta no
            <strong>GlicoNutri</strong>.</p>
            <p><a href="{link}">Definir uma nova senha</a></p>
            <p>O link vale por 1 hora e só pode ser usado uma vez. Se não foi você
            quem pediu, ignore esta mensagem — sua senha atual continua valendo.</p>
            """;

        return EnviarAsync(email, nome, "Redefinição de senha — GlicoNutri", corpo, ct);
    }

    private async Task EnviarAsync(
        string destino, string nomeDestino, string assunto, string corpoHtml, CancellationToken ct)
    {
        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(_opcoes.RemetenteNome, _remetente));
        mensagem.To.Add(new MailboxAddress(nomeDestino, destino));
        mensagem.Subject = assunto;
        mensagem.Body = new BodyBuilder { HtmlBody = corpoHtml }.ToMessageBody();

        try
        {
            using var cliente = new SmtpClient();

            await cliente.ConnectAsync(
                _host, _opcoes.Porta,
                _opcoes.UsarSslDireto ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                ct);

            if (!string.IsNullOrWhiteSpace(_opcoes.Usuario))
                await cliente.AuthenticateAsync(_opcoes.Usuario, _opcoes.Senha ?? string.Empty, ct);

            await cliente.SendAsync(mensagem, ct);
            await cliente.DisconnectAsync(true, ct);

            _log.LogInformation("E-mail \"{Assunto}\" enviado para {Destino}.", assunto, destino);
        }
        catch (Exception e)
        {
            // Uma falha de SMTP não pode derrubar o cadastro que já foi gravado:
            // a conta existe, e a senha provisória pode ser reenviada depois pelo
            // fluxo de recuperação.
            _log.LogError(e, "Falha ao enviar \"{Assunto}\" para {Destino}.", assunto, destino);
        }
    }

    private static string Escapar(string texto) =>
        System.Net.WebUtility.HtmlEncode(texto);
}
