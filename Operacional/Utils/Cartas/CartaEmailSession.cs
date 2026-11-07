using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace Operacional.Utils.Cartas;

internal sealed class CartaEmailSession : IDisposable
{
    private const string Host = "webmail.cipolatti.com.br";
    private readonly SmtpClient smtp = new() { Timeout = 90000 };
    private readonly ImapClient imap = new() { Timeout = 90000 };
    private IMailFolder? enviados;

    public async Task ConectarAsync(string email, string senha, string pastaEnviados, bool somenteCopia = false)
    {
        if (string.IsNullOrWhiteSpace(senha)) throw new InvalidOperationException("Informe a senha do email.");
        // Nao substituir a validacao de certificados TLS.
        await imap.ConnectAsync(Host, 993, SecureSocketOptions.SslOnConnect);
        await imap.AuthenticateAsync(email, senha);
        if (!string.IsNullOrWhiteSpace(pastaEnviados))
            enviados = await imap.GetFolderAsync(pastaEnviados.Trim());
        else
        {
            if (imap.Capabilities.HasFlag(ImapCapabilities.SpecialUse) || imap.Capabilities.HasFlag(ImapCapabilities.XList))
                enviados = imap.GetFolder(SpecialFolder.Sent);
            if (enviados == null)
                throw new InvalidOperationException(
                    "O servidor nao identificou a pasta Enviados. Informe o nome completo da pasta no campo Pasta Enviados.");
        }
        await enviados.OpenAsync(FolderAccess.ReadWrite);
        if (!somenteCopia)
        {
            await smtp.ConnectAsync(Host, 465, SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync(email, senha);
        }
    }

    public Task<string> EnviarAsync(MimeMessage message) => smtp.SendAsync(message);

    public async Task SalvarCopiaAsync(MimeMessage message)
    {
        if (enviados == null) throw new InvalidOperationException("Pasta Enviados nao conectada.");
        // Evita duplicar a copia se o servidor ja a salvou ou o APPEND anterior perdeu a resposta.
        var existentes = await enviados.SearchAsync(SearchQuery.HeaderContains("Message-Id", message.MessageId));
        if (existentes.Count == 0)
            await enviados.AppendAsync(message, MessageFlags.Seen, message.Date);
    }

    public void Dispose()
    {
        smtp.Dispose();
        imap.Dispose();
    }
}
