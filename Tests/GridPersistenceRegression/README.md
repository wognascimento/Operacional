# Grid Persistence Regression

Teste local do ciclo de edicao do Telerik usado pelo Operacional. Nao conecta
ao banco e nao altera dados reais. Usa uma janela fora da area visivel,
fechada ao terminar.

Primeiro compile o Operacional em Release. O teste referencia os mesmos
assemblies Telerik gerados pelo projeto, sem baixar outra versao.

```powershell
dotnet build Operacional/Operacional/Operacional.csproj -c Release --no-restore
dotnet restore Operacional/Tests/GridPersistenceRegression/GridPersistenceRegression.csproj --source Operacional/Tests/GridPersistenceRegression -p:NuGetAudit=false
dotnet run --project Operacional/Tests/GridPersistenceRegression/GridPersistenceRegression.csproj -c Release --no-restore
```

Verifica:

- A linha nao e confirmada enquanto a persistencia esta pendente.
- Repetir a confirmacao nao dispara gravacoes concorrentes.
- Falha de persistencia nao confirma a linha.
- ESC restaura o valor anterior depois de uma falha.
- Cancelar antes de iniciar o envio impede a gravacao enfileirada.
- Uma nova tentativa depois da falha funciona.
- Inserir uma linha nova nao duplica a gravacao.

Os comandos SQL, triggers e permissoes precisam ser validados separadamente
em um banco de homologacao.
