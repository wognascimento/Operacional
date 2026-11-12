# Operacional

## Visao geral

Planejamento e acompanhamento das operacoes de montagem, manutencao e desmontagem.

- Transportes, cargas e cronograma operacional.
- Despesas, pagamentos e equipe externa.
- Cadastro de usuarios, envio ao aplicativo e presenca manual.
- Programacao de manutencao e cartas com envio por e-mail.

## Ambiente de desenvolvimento

- Windows e SDK .NET 10.
- Pacotes e licenca Telerik UI for WPF configurados localmente.
- PostgreSQL com os objetos e permissoes exigidos pelo modulo.
- PowerShell 7 e Inno Setup 6 para gerar instaladores.

Mantenha este repositorio, `BibliotecasSIG` e `Update` como pastas irmas. Os arquivos de projeto usam referencias relativas entre esses repositorios.

O projeto principal e `Operacional/Operacional.csproj`. Abra `Operacional.sln` ou execute na raiz do repositorio:

```powershell
dotnet restore Operacional.sln
dotnet build Operacional.sln -c Release
```

As versoes exatas das dependencias estao no arquivo de projeto. Configure o feed Telerik sem versionar senhas ou tokens.

## Configuracao e cuidados

Confira `Operacional/App.config` e a classe de configuracao de banco do modulo. Defina host, banco do exercicio e credenciais adequadas ao ambiente. A compilacao nao cria tabelas nem concede permissoes PostgreSQL.

Nao inclua senhas, tokens, arquivos pessoais ou exportacoes reais em commits. Valide consultas e gravacoes em homologacao com uma conta autorizada antes de distribuir.

## Testes e homologacao

Ha verificacoes em `Tests/CartasEnvio/CartasEnvio.csproj`:

```powershell
dotnet run --project Tests/CartasEnvio/CartasEnvio.csproj -c Release
```

Leia o teste e suas dependencias antes de executar. Confira abertura das telas, filtros, consultas e fluxos modificados. Integracoes, impressoes e gravacoes precisam de validacao especifica; um build aprovado nao garante acesso ao banco ou aos servicos externos.

## Distribuicao

`deploy.ps1`, `Setup.iss` e `scripts/` implementam a publicacao. O servidor padrao e `https://atualizasig.cipolatti.com.br`.

Mantenha `Version`, `AssemblyVersion` e `FileVersion` iguais no projeto. Para gerar os artefatos sem enviar ao servidor:

```powershell
./deploy.ps1 -Changelog @("Descricao das alteracoes") -SkipServerUpload
```

Para enviar, execute sem `-SkipServerUpload` usando credenciais autorizadas. A publicacao gera um rascunho para homologacao; a liberacao ocorre na Central. Consulte os parametros do script para caminho do Inno Setup e envio de artefatos existentes.

Push Git envia codigo/documentacao; nao equivale a publicar uma versao do aplicativo. Nao versione `bin/`, `obj/`, pacotes ou instaladores gerados.
