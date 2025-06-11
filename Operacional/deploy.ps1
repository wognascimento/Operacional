# Script de Deploy Automatizado

# Configurações
$projectPath = $PSScriptRoot
$outputPath = "$projectPath\bin\Release"
$publishPath = "$projectPath\publish"
$serverUploadPath = "root@192.168.0.49:/var/www/updates/downloads/operacional/"
$InnoCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

# Função para extrair versão do arquivo de projeto
function Get-ApplicationVersion {
    # Tenta encontrar o arquivo .csproj
    $projFile = Get-ChildItem -Path $projectPath -Filter *.csproj | Select-Object -First 1
    #if ($projFile) {
        # Ler conteúdo do arquivo de projeto
        $projContent = [xml](Get-Content $projFile.FullName)
        
        # Tentar extrair versão de diferentes formas
        #$version = $projContent.Project.PropertyGroup.Version
        #if (-not $version) {
            $version = $projContent.Project.PropertyGroup.AssemblyVersion
        #}
        return $version
    #}
    # Fallback: versão manual
    #return "1.0.0"
}

# Limpar pastas anteriores
if (Test-Path $publishPath) {
    Remove-Item $publishPath -Recurse -Force
}

# Criar pasta de publicação
New-Item -ItemType Directory -Path $publishPath -Force

# Obter versão atual
$version = Get-ApplicationVersion
$zipFileName = "application-$version.zip"
$zipFullPath = "$projectPath\$zipFileName"
$issFile = "$projectPath\Setup.iss"

# Carrega todo o conteúdo do arquivo
$content = Get-Content $issFile
# Altera a linha que contém '#define MyAppVersion'
$content = $content -replace '(?m)^#define\s+MyAppVersion\s+".*"$', "#define MyAppVersion `"$version`""

# Salva o conteúdo de volta no arquivo
Set-Content -Path $issFile -Value $content

# Compilar o projeto
dotnet publish $projectPath -c Release -o $publishPath

if (Test-Path $InnoCompiler) {
    & $InnoCompiler "Setup.iss"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Compilação concluída com sucesso!"
    } else {
        Write-Host "Erro na compilação do instalador"
    }
} else {
    Write-Host "Compilador do Inno Setup não encontrado"
}

# Zipar a aplicação
Compress-Archive -Path "$publishPath\*" -DestinationPath $zipFullPath -Force

# Gerar JSON de atualização
$updateJson = @{
    currentVersion = $version
    updateVersion = $version
    updateUrl = "http://192.168.0.49/downloads/operacional/$zipFileName"
    changelog = @(
        "Ajustes",
        "Melhorias"
    )
    releaseDate = (Get-Date).ToString("yyyy-MM-dd")
    minimumCompatibleVersion = "1.0.0"
} | ConvertTo-Json

# Salvar JSON com codificação UTF-8 sem BOM
#$Utf8NoBomEncoding = New-Object System.Text.UTF8Encoding $False
#[System.IO.File]::WriteAllLines("$projectPath\version.json", $updateJson, $Utf8NoBomEncoding)

# Salvar JSON
$updateJson | Out-File -FilePath "$projectPath\version.json"

# Upload para o servidor usando SCP
# Requer que o SSH esteja configurado
scp $zipFullPath $serverUploadPath
scp "$projectPath\version.json" "root@192.168.0.49:/var/www/updates/downloads/operacional/version.json"

$credencial = Get-Credential -UserName "cipodominio\administrador"
New-PSDrive -Name "RedeTemp" -PSProvider FileSystem -Root "\\192.168.0.4\sistemas\SIG" -Credential $credencial
Copy-Item -Path "$projectPath\Output\Operacional.exe"  -Destination "RedeTemp:"
Remove-PSDrive -Name "RedeTemp"


# Opcional: Remover arquivos temporários
Remove-Item $zipFullPath
Remove-Item "$projectPath\version.json"

Write-Host "Deploy concluído para versão $version"