# Carta Inicio Montagem

Teste local, sem acesso ao banco e sem abrir o Word.

Compile primeiro o Operacional em Release. Depois execute na raiz do workspace:

```powershell
dotnet restore Operacional/Tests/CartaMontagemRegression/CartaMontagemRegression.csproj --source Operacional/Tests/CartaMontagemRegression -p:NuGetAudit=false
dotnet run --project Operacional/Tests/CartaMontagemRegression/CartaMontagemRegression.csproj -c Release --no-restore -- Operacional/Operacional/Modelos/MODELO_CARTA_INICIO_MONTAGEM.docx Operacional/Tests/CartaMontagemRegression/bin/carta-teste
```

O teste usa dados ficticios e verifica:

- Datas, numeros, zero, valores nulos e identificadores longos no Excel.
- Texto iniciado por sinal de igual permanece texto, nao formula.
- Fonte da mala direta aponta para o Excel junto da copia da carta.
- Consulta da mala direta usa a aba gerada.
- Todas as partes do DOCX, exceto as duas configuracoes de conexao,
  permanecem identicas ao modelo, incluindo texto, imagens, estilos e layout.
- O modelo original permanece intacto.

Homologacao com o sistema e Word:

1. Acessar Carta > Inicio Montagem.
2. Verificar campos de datas obrigatorios e rejeicao de periodo invertido.
3. Conferir registros do primeiro e ultimo dia, incluindo horarios no ultimo dia.
4. Conferir mensagem para periodo sem registros.
5. Abrir a carta gerada e confirmar os destinatarios da mala direta.
6. Gerar outro periodo e confirmar que os arquivos anteriores nao foram substituidos.

O filtro SQL utiliza datas parametrizadas: maior ou igual ao inicio e menor
que o dia posterior ao fim. O teste local nao valida permissoes, existencia
da view no banco nem a disponibilidade do provedor ACE no Word instalado.
