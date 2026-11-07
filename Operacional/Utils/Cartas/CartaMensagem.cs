using HtmlAgilityPack;
using MimeKit;
using MimeKit.Utils;
using System.IO;
using System.Text;

namespace Operacional.Utils.Cartas;

internal static class CartaMensagem
{
    public static MimeMessage Criar(CartaDestinatario item, string remetente, string resposta, string assunto)
    {
        if (string.IsNullOrWhiteSpace(assunto) || assunto.Contains('\r') || assunto.Contains('\n'))
            throw new InvalidOperationException("Informe um assunto valido.");
        if (item.Html == null || item.Pdf == null)
            throw new InvalidOperationException("Gere a carta antes de enviar.");
        var subject = CartaDestinatario.ModoTeste ? "[TESTE] " + assunto.Trim() : assunto.Trim();
        var message = new MimeMessage { Subject = subject, Date = DateTimeOffset.Now, MessageId = MimeUtils.GenerateMessageId() };
        message.From.Add(MailboxAddress.Parse(CartaDestinatario.NormalizarEmail(remetente)));
        message.To.Add(MailboxAddress.Parse(item.EmailEnvio));
        message.ReplyTo.Add(MailboxAddress.Parse(CartaDestinatario.NormalizarEmail(resposta)));
        var body = new BodyBuilder();
        var document = new HtmlDocument();
        document.Load(item.Html, Encoding.UTF8);
        foreach (var node in document.DocumentNode.SelectNodes("//script|//iframe|//object|//link|//base")?.ToArray() ?? [])
            node.Remove();
        var pasta = Path.GetFullPath(Path.GetDirectoryName(item.Html)!) + Path.DirectorySeparatorChar;
        var resources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in document.DocumentNode.Descendants().ToArray())
        {
            foreach (var attr in node.Attributes.ToArray())
            {
                if (attr.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                    node.Attributes.Remove(attr);
            }
            var src = node.Attributes["src"];
            if (src == null) continue;
            var value = HtmlEntity.DeEntitize(src.Value);
            var uri = new Uri(new Uri(item.Html), value);
            if (!uri.IsFile)
                throw new InvalidOperationException("Imagem externa no modelo. Incorpore a imagem ao Word antes de enviar.");
            var path = Path.GetFullPath(uri.LocalPath);
            if (!path.StartsWith(pasta, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                throw new InvalidOperationException("Imagem da carta ausente ou fora da pasta gerada.");
            if (!resources.TryGetValue(path, out var cid))
            {
                var resource = body.LinkedResources.Add(path);
                resource.ContentId = cid = MimeUtils.GenerateMessageId();
                resources.Add(path, cid);
            }
            src.Value = "cid:" + cid;
        }
        body.HtmlBody = document.DocumentNode.OuterHtml;
        body.TextBody = HtmlEntity.DeEntitize(document.DocumentNode.SelectSingleNode("//body")?.InnerText
            ?? document.DocumentNode.InnerText);
        body.Attachments.Add(item.Pdf);
        message.Body = body.ToMessageBody();
        return message;
    }
}
