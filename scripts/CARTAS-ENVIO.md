# Envio de cartas

## Homologacao concluida

CartaDestinatario.ModoTeste esta false: as mensagens usam os destinatarios do grid,
sem prefixo [TESTE] no assunto. A pasta de enviados vem preenchida com Sent.
Cada linha marcada gera uma mensagem. O historico anterior de homologacao,
com tipos TESTE_*, permanece separado e nao marca os envios reais como concluidos.

## Instalacao

1. Executar `20261030_cartas_envio.sql` em cada banco anual usado pelo Operacional.
2. Conceder SELECT, INSERT e UPDATE nas duas tabelas aos usuarios/roles autorizados do SIG.
   Nao conceder permissao a PUBLIC. A aplicacao nao cria tabelas automaticamente.
3. Publicar o Operacional com as novas dependencias e os quatro modelos Word.
4. Instalar/ativar o Word nas estacoes. Outlook e Thunderbird nao sao necessarios.
5. Liberar TLS para webmail.cipolatti.com.br:465 (SMTP) e :993 (IMAP).

## Uso

- Gerar carta nas telas Inicio Montagem, Transporte Montagem, Apoio Montagem ou Manutencao.
- A janela de envio mostra a lista da consulta, com os pendentes validos selecionados.
- Informar remetente, senha, Responder para e assunto. A senha nao e persistida e e limpa apos o lote.
- Conferir PDF e HTML da linha antes de enviar. PDF preserva o layout Word; HTML depende do leitor de email.
- Enviar selecionados considera apenas as linhas visiveis apos os filtros do grid.
- Para repetir, marcar as linhas ja enviadas e usar Enviar selecionados. A confirmacao
  informa quais cartas serao reenviadas; as demais continuam sendo envios novos.
- Se o servidor nao anunciar a pasta especial Sent, informar o caminho IMAP exato em Pasta Enviados.
- Salvar copia em Enviados atua na linha atual, sem novo envio SMTP, usando a conta remetente original.
- Parar interrompe apos o item atual. Nao fecha o programa durante o envio.

## Controle

A identidade e tipo + ano configurado + sigla normalizada + email normalizado.
Envios antigos feitos pelo Word/Outlook nao sao importados automaticamente: o controle
passa a valer para os envios realizados por esta nova tela.
O periodo nao faz parte da identidade. Registros identicos sao agrupados;
registros com conteudos diferentes para a mesma identidade sao bloqueados para revisao na origem.
Enderecos separados por ponto e virgula geram mensagens individuais.

O banco registra cada tentativa e o MIME completo (HTML, imagens e PDF), sem senha.
Trava consultiva PostgreSQL e verificacao da tentativa anterior impedem dois envios
simultaneos ou usando uma lista desatualizada. Nao ha repeticao automatica de SMTP.

Enviado significa aceito pelo servidor SMTP, nao confirmacao de leitura/entrega final.
Falha significa rejeicao SMTP explicita. Incerto/enviando pode significar que o SMTP
aceitou antes de uma queda de conexao/processo: nao reenviar sem verificar.
Reenvio explicito fica no historico e nunca apaga tentativas anteriores.
Uma falha no reenvio nao volta a selecionar automaticamente uma carta enviada anteriormente.

Salvar em Enviados e separado do envio. A recuperacao procura o mesmo Message-Id
antes do APPEND para evitar duplicar a copia. O servidor deve permitir busca/gravar
na pasta Enviados. Se o banco ficar indisponivel apos o SMTP aceitar, parar e conferir
com o suporte; nao presumir que o email nao saiu.

As tabelas contem correspondencia e dados pessoais. Aplicar permissoes restritas,
backup e politica de retencao da empresa. Nao apagar registros para permitir reenvio.

## Validacao

Executar `dotnet run --project Tests/CartasEnvio/CartasEnvio.csproj`.
Os testes usam dados sinteticos, sem banco ou envio real.
Homologar com conta de teste: Word/PDF/HTML, Reply-To, copia IMAP, duas estacoes
simultaneas, reenvio confirmado, indisponibilidade IMAP e queda durante SMTP.
Nao usar destinatarios de clientes na homologacao.
