using MailKit.Net.Smtp;
using System.IO;

namespace Operacional.Utils.Cartas;

internal static class CartaEnvioService
{
    public static async Task EnviarAsync(CartaDestinatario item, CartaEnvioRepository repository,
        CartaEmailSession session, string remetente, string resposta, string assunto, bool reenvio)
    {
        if (!item.Valido) throw new InvalidOperationException(item.Problema);
        using var mensagem = CartaMensagem.Criar(item, remetente, resposta, assunto);
        using var stream = new MemoryStream();
        await mensagem.WriteToAsync(stream);
        await using var reserva = await repository.ReservarAsync(item, reenvio, remetente,
            resposta, mensagem.Subject, mensagem.MessageId, stream.ToArray());
        item.Tentativa = reserva.Id;
        item.Status = "enviando";
        item.Selecionado = false;
        var aceito = false;
        try
        {
            await session.EnviarAsync(mensagem);
            aceito = true;
            item.Status = "enviado";
            item.JaEnviado = true;
            await reserva.ConcluirAsync("enviado");
        }
        catch (Exception ex)
        {
            if (aceito)
                throw new InvalidOperationException(
                    "O servidor aceitou o email, mas o historico nao foi confirmado. Nao reenvie; solicite verificacao ao suporte.");
            var rejeitado = ex is SmtpCommandException smtp &&
                (int)smtp.StatusCode >= 400 && (int)smtp.StatusCode < 600;
            item.Status = rejeitado ? "falha" : "incerto";
            item.Detalhe = rejeitado ? "O servidor SMTP rejeitou a mensagem." :
                "A conexao falhou durante o envio. Confira com o suporte antes de reenviar.";
            await reserva.ConcluirAsync(item.Status, item.Detalhe);
            throw new InvalidOperationException(item.Detalhe);
        }
        try
        {
            await session.SalvarCopiaAsync(mensagem);
            await repository.RegistrarCopiaAsync(reserva.Id, "salva", null);
            item.Copia = "salva";
            item.Detalhe = "";
        }
        catch
        {
            item.Copia = "falha";
            item.Detalhe = "Email enviado. Copia em Enviados nao confirmada. Use Salvar copia, sem reenviar.";
            await repository.RegistrarCopiaAsync(reserva.Id, "falha", item.Detalhe);
        }
    }
}
