using Dapper;
using Npgsql;
using System.Data;

namespace Operacional.Utils.Cartas;

internal sealed class CartaEnvioRepository(string connectionString, string usuario)
{
    private NpgsqlConnection Connection() => new(connectionString);

    public async Task AtualizarAsync(IReadOnlyCollection<CartaDestinatario> itens)
    {
        await using var conn = Connection();
        if (!await conn.ExecuteScalarAsync<bool>("""
            SELECT to_regclass('operacional.carta_envio') IS NOT NULL
               AND to_regclass('operacional.carta_envio_tentativa') IS NOT NULL;
            """))
            throw new InvalidOperationException(
                "Controle de cartas nao instalado neste banco. Solicite ao administrador a execucao de scripts/20261030_cartas_envio.sql e as permissoes de acesso.");
        var estados = (await conn.QueryAsync<Estado>("""
            SELECT c.chave, c.status, c.ultima_tentativa AS tentativa,
                t.copia_status AS copia, t.detalhe, t.copia_detalhe AS copiaDetalhe, t.usuario,
                (SELECT max(h.enviado_em) FROM operacional.carta_envio_tentativa h
                 WHERE h.chave=c.chave) AS ultimoEnvio
            FROM operacional.carta_envio c
            LEFT JOIN operacional.carta_envio_tentativa t ON t.id=c.ultima_tentativa
            WHERE c.chave = ANY(@chaves);
            """, new { chaves = itens.Select(i => i.Chave).ToArray() })).ToDictionary(e => e.Chave);
        foreach (var item in itens)
        {
            estados.TryGetValue(item.Chave, out var estado);
            item.Status = estado?.Status ?? "pendente";
            item.Copia = estado?.Copia ?? "";
            item.Tentativa = estado?.Tentativa;
            item.JaEnviado = estado?.UltimoEnvio != null;
            item.UltimoEnvio = estado?.UltimoEnvio?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") ?? "";
            item.Usuario = estado?.Usuario ?? "";
            item.Detalhe = !item.Valido ? item.Problema :
                string.Join(" ", new[] { estado?.Detalhe, estado?.CopiaDetalhe }.Where(s => !string.IsNullOrEmpty(s)));
            item.Selecionado = CartaDestinatario.SelecionarPorPadrao(item.Status, item.JaEnviado, item.Valido);
        }
    }

    public async Task<Reserva> ReservarAsync(CartaDestinatario item, bool reenvio,
        string remetente, string resposta, string assunto, string messageId, byte[] mensagem)
    {
        var conn = Connection();
        await conn.OpenAsync();
        var locked = false;
        try
        {
            locked = await conn.ExecuteScalarAsync<bool>(
                "SELECT pg_try_advisory_lock(hashtextextended(@chave, 0));", new { item.Chave });
            if (!locked) throw new InvalidOperationException("Esta carta esta sendo enviada em outra janela/estacao.");
            await using var transaction = await conn.BeginTransactionAsync();
            await conn.ExecuteAsync("""
                INSERT INTO operacional.carta_envio(chave,tipo,ano,sigla,destinatario)
                VALUES (@chave,@tipo,@ano,@sigla,@email) ON CONFLICT DO NOTHING;
                """, item, transaction);
            var estado = await conn.QuerySingleAsync<Estado>("""
                SELECT status, ultima_tentativa AS tentativa FROM operacional.carta_envio
                WHERE chave=@chave FOR UPDATE;
                """, new { item.Chave }, transaction);
            var enviado = await conn.ExecuteScalarAsync<bool>("""
                SELECT EXISTS(SELECT 1 FROM operacional.carta_envio_tentativa
                WHERE chave=@chave AND enviado_em IS NOT NULL);
                """, new { item.Chave }, transaction);
            // Reconfirma a versao vista no grid: dois cliques/janelas nao autorizam dois envios.
            if (estado.Tentativa != item.Tentativa)
                throw new InvalidOperationException("O historico mudou. Atualize a lista e confira antes de enviar.");
            if (!reenvio && (enviado || !CartaDestinatario.Pendente(estado.Status)))
                throw new InvalidOperationException("Carta ja enviada ou com resultado incerto. Atualize a lista, marque a carta e confirme o reenvio.");
            var id = Guid.NewGuid();
            await conn.ExecuteAsync("""
                INSERT INTO operacional.carta_envio_tentativa
                (id,chave,usuario,remetente,resposta,assunto,message_id,reenvio,status,mensagem)
                VALUES (@id,@chave,@usuario,@remetente,@resposta,@assunto,@messageId,@reenvio,'enviando',@mensagem);
                UPDATE operacional.carta_envio SET status='enviando',ultima_tentativa=@id,atualizado_em=now()
                WHERE chave=@chave;
                """, new { id, item.Chave, usuario, remetente, resposta, assunto, messageId, reenvio, mensagem }, transaction);
            await transaction.CommitAsync();
            return new Reserva(conn, item.Chave, id);
        }
        catch
        {
            if (locked)
                try { await conn.ExecuteAsync("SELECT pg_advisory_unlock(hashtextextended(@chave,0));", new { item.Chave }); }
                catch { /* A conexao sera descartada. */ }
            await conn.DisposeAsync();
            throw;
        }
    }

    public async Task<DataTable> HistoricoAsync(string chave)
    {
        await using var conn = Connection();
        using var reader = await conn.ExecuteReaderAsync("""
            SELECT iniciado_em, enviado_em, usuario, remetente, resposta, assunto, reenvio,
                status, copia_status, detalhe, copia_detalhe, message_id
            FROM operacional.carta_envio_tentativa WHERE chave=@chave ORDER BY iniciado_em DESC;
            """, new { chave });
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    public async Task<CopiaPendente> ObterCopiaAsync(Guid id)
    {
        await using var conn = Connection();
        return await conn.QuerySingleAsync<CopiaPendente>("""
            SELECT mensagem, remetente FROM operacional.carta_envio_tentativa
            WHERE id=@id AND status='enviado';
            """, new { id });
    }

    public async Task RegistrarCopiaAsync(Guid id, string status, string? detalhe)
    {
        await using var conn = Connection();
        await conn.ExecuteAsync("""
            UPDATE operacional.carta_envio_tentativa SET copia_status=@status,copia_detalhe=@detalhe
            WHERE id=@id AND status='enviado';
            """, new { id, status, detalhe });
    }

    public async Task RecuperarCopiaAsync(Guid id, Func<CopiaPendente, Task> salvar)
    {
        await using var conn = Connection();
        await conn.OpenAsync();
        var chave = await conn.QuerySingleAsync<string>(
            "SELECT chave FROM operacional.carta_envio_tentativa WHERE id=@id AND status='enviado';", new { id });
        if (!await conn.ExecuteScalarAsync<bool>(
            "SELECT pg_try_advisory_lock(hashtextextended(@chave,0));", new { chave }))
            throw new InvalidOperationException("Esta carta esta em uso por outra estacao.");
        try
        {
            var copia = await ObterCopiaAsync(id);
            await salvar(copia);
            await RegistrarCopiaAsync(id, "salva", null);
        }
        finally { await conn.ExecuteAsync("SELECT pg_advisory_unlock(hashtextextended(@chave,0));", new { chave }); }
    }

    internal sealed class Reserva(NpgsqlConnection conn, string chave, Guid id) : IAsyncDisposable
    {
        public Guid Id => id;
        public async Task ConcluirAsync(string status, string? detalhe = null)
        {
            await using var transaction = await conn.BeginTransactionAsync();
            await conn.ExecuteAsync("""
                UPDATE operacional.carta_envio_tentativa SET status=@status,detalhe=@detalhe,
                    enviado_em=CASE WHEN @status='enviado' THEN now() ELSE enviado_em END WHERE id=@id;
                UPDATE operacional.carta_envio SET status=@status,atualizado_em=now()
                WHERE chave=@chave AND ultima_tentativa=@id;
                """, new { id, chave, status, detalhe }, transaction);
            await transaction.CommitAsync();
        }
        public async ValueTask DisposeAsync()
        {
            try { await conn.ExecuteAsync("SELECT pg_advisory_unlock(hashtextextended(@chave,0));", new { chave }); }
            finally { await conn.DisposeAsync(); }
        }
    }

    private sealed class Estado
    {
        public string Chave { get; set; } = "";
        public string Status { get; set; } = "";
        public Guid? Tentativa { get; set; }
        public string? Copia { get; set; }
        public string? Detalhe { get; set; }
        public string? CopiaDetalhe { get; set; }
        public string? Usuario { get; set; }
        public DateTime? UltimoEnvio { get; set; }
    }
    internal sealed class CopiaPendente
    {
        public byte[] Mensagem { get; set; } = [];
        public string Remetente { get; set; } = "";
    }
}
