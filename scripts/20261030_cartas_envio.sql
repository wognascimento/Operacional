-- Executar em cada banco anual utilizado pelo Operacional, como administrador.
-- Nao altera as views de origem. Conceder acesso somente aos usuarios/roles do SIG.
BEGIN;
CREATE TABLE IF NOT EXISTS operacional.carta_envio (
    chave text PRIMARY KEY,
    tipo text NOT NULL,
    ano text NOT NULL,
    sigla text NOT NULL,
    destinatario text NOT NULL,
    status text NOT NULL DEFAULT 'pendente'
        CHECK (status IN ('pendente','enviando','enviado','falha','incerto')),
    ultima_tentativa uuid,
    atualizado_em timestamptz NOT NULL DEFAULT now(),
    UNIQUE (tipo, ano, sigla, destinatario)
);
CREATE TABLE IF NOT EXISTS operacional.carta_envio_tentativa (
    id uuid PRIMARY KEY,
    chave text NOT NULL REFERENCES operacional.carta_envio(chave),
    usuario text NOT NULL,
    remetente text NOT NULL,
    resposta text NOT NULL,
    assunto text NOT NULL,
    message_id text NOT NULL UNIQUE,
    reenvio boolean NOT NULL,
    status text NOT NULL CHECK (status IN ('enviando','enviado','falha','incerto')),
    iniciado_em timestamptz NOT NULL DEFAULT now(),
    enviado_em timestamptz,
    copia_status text NOT NULL DEFAULT 'pendente'
        CHECK (copia_status IN ('pendente','salva','falha')),
    detalhe text,
    copia_detalhe text,
    -- MIME sem senha: permite recuperar a copia em Enviados em outra estacao.
    mensagem bytea NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_carta_envio_tentativa_chave
    ON operacional.carta_envio_tentativa (chave, iniciado_em DESC);
COMMIT;

-- Substituir role_sig pela role real usada no ambiente (nao executar literalmente):
-- GRANT SELECT, INSERT, UPDATE ON operacional.carta_envio,
--     operacional.carta_envio_tentativa TO role_sig;
-- As tabelas armazenam conteudo de correspondencia: restringir acesso e incluir
-- na politica de backup/retencao. Nao ha senha de email nestas tabelas.
