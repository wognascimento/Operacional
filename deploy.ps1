#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$ServerUrl = 'https://atualizasig.cipolatti.com.br',
    [PSCredential]$Credential,
    [string[]]$Changelog,
    [string]$MinimumCompatibleVersion = '1.0.0.0',
    [string]$InnoCompiler = '',
    [string]$DotNetDesktopRuntimeInstallerPath = '',
    [switch]$SkipRuntimeBundle,
    [switch]$SkipServerUpload,
    [switch]$UploadOnly,
    [string]$ArtifactDirectory,
    [switch]$CopyPublishedInstaller,
    [string]$NetworkDeployPath = '\\192.168.0.4\sistemas\SIG',
    [PSCredential]$NetworkCredential,
    # Compatibility with earlier invocations; network copying is now opt-in.
    [switch]$SkipNetworkCopy,
    [switch]$ForceDeploy,
    [string]$ServerUploadPath,
    [string]$UpdateBaseUrl
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'scripts\UpdatePublishing.psm1') -Force
if ($ForceDeploy -or $ServerUploadPath -or $UpdateBaseUrl) { throw 'ForceDeploy/ServerUploadPath/UpdateBaseUrl nao se aplicam a Central. Use ServerUrl e uma versao nova; pacotes publicados sao imutaveis.' }
if ($UploadOnly -and $SkipServerUpload) { throw 'UploadOnly nao pode ser combinado com SkipServerUpload.' }
if ($CopyPublishedInstaller -and ($UploadOnly -or $SkipNetworkCopy)) { throw 'CopyPublishedInstaller deve ser usado sozinho, com ArtifactDirectory e NetworkDeployPath.' }
if ($ArtifactDirectory -and -not ($UploadOnly -or $CopyPublishedInstaller)) { throw 'ArtifactDirectory e usado apenas com UploadOnly ou CopyPublishedInstaller.' }

$projectPath = [IO.Path]::GetFullPath($PSScriptRoot)
$workspaceRoot = Split-Path $projectPath -Parent
$projectFile = Join-Path $projectPath 'Operacional\Operacional.csproj'
$versionsPath = Join-Path $projectPath 'artifacts\versions'
$stagingRoot = Join-Path $projectPath 'artifacts\.staging'
$session = $null
$stage = $null

function Invoke-NativeCommand {
    param([string]$FilePath, [string[]]$Arguments)
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Comando falhou (codigo $LASTEXITCODE): $FilePath" }
}
function Get-ApplicationVersion {
    $xml = [xml](Get-Content -LiteralPath $projectFile -Raw)
    $assemblyNode = $xml.SelectSingleNode('/Project/PropertyGroup/AssemblyVersion')
    if ($null -eq $assemblyNode) { throw 'Defina AssemblyVersion no projeto antes de gerar a versao.' }
    $v = ConvertTo-SigVersion $assemblyNode.InnerText
    foreach ($name in @('Version', 'FileVersion')) {
        $node = $xml.SelectSingleNode("/Project/PropertyGroup/$name")
        if ($null -eq $node -or (ConvertTo-SigVersion $node.InnerText) -ne $v) { throw "Version, AssemblyVersion e FileVersion devem indicar a mesma versao. Confira $name no .csproj." }
    }
    return $v
}
function Resolve-InnoCompiler {
    if ($InnoCompiler) { return (Resolve-Path -LiteralPath $InnoCompiler -ErrorAction Stop).Path }
    foreach ($candidate in @((Join-Path $workspaceRoot 'tools\InnoSetup6\ISCC.exe'), 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe')) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
    }
    throw 'ISCC.exe nao encontrado. Informe -InnoCompiler ou use tools\InnoSetup6\ISCC.exe no workspace.'
}
function Resolve-RuntimeInstaller {
    if ($SkipRuntimeBundle) { return $null }
    if ($DotNetDesktopRuntimeInstallerPath) { return (Resolve-Path -LiteralPath $DotNetDesktopRuntimeInstallerPath -ErrorAction Stop).Path }
    $directory = Join-Path $workspaceRoot 'tools\dotnet'
    $candidates = @(Get-ChildItem -LiteralPath $directory -Filter 'windowsdesktop-runtime-10.*-win-x64.exe' -File -ErrorAction SilentlyContinue |
        Where-Object Name -Match '^windowsdesktop-runtime-10\.0\.\d+-win-x64\.exe$' |
        Sort-Object { [version]($_.BaseName -replace '^windowsdesktop-runtime-', '' -replace '-win-x64$', '') } -Descending)
    if ($candidates.Count) { return $candidates[0].FullName }
    $fixedName = Join-Path $directory 'windowsdesktop-runtime-10.0-win-x64.exe'
    if (Test-Path -LiteralPath $fixedName -PathType Leaf) { return $fixedName }
    throw 'Instalador .NET Desktop Runtime 10 x64 nao encontrado em tools\dotnet. Informe -DotNetDesktopRuntimeInstallerPath ou use -SkipRuntimeBundle somente se o runtime ja estiver instalado nas estacoes.'
}
function Remove-StagingDirectory {
    param([string]$Path)
    $resolvedRoot = [IO.Path]::GetFullPath($stagingRoot).TrimEnd('\') + '\'
    $resolvedTarget = [IO.Path]::GetFullPath($Path)
    if (-not $resolvedTarget.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase) -or $resolvedTarget -eq $resolvedRoot.TrimEnd('\')) { throw "Limpeza recusada fora de artifacts\.staging: $resolvedTarget" }
    if ((Get-Item -LiteralPath $resolvedTarget).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Limpeza recusada em link de diretorio.' }
    Remove-Item -LiteralPath $resolvedTarget -Recurse -Force
}

try {
    if ($UploadOnly -or $CopyPublishedInstaller) {
        if (-not $ArtifactDirectory) { throw 'Informe -ArtifactDirectory com a pasta que contem release.json, ZIP e instalador.' }
        $release = Read-SigRelease -ArtifactDirectory $ArtifactDirectory
        if ($CopyPublishedInstaller) {
            Copy-SigPublishedInstaller -Release $release -ServerUrl $ServerUrl -Destination $NetworkDeployPath -Credential $NetworkCredential
            return
        }
        $version = $release.Metadata.version
    } else {
        $version = Get-ApplicationVersion
        $minimum = ConvertTo-SigVersion $MinimumCompatibleVersion
        if ([version]$minimum -gt [version]$version) { throw 'MinimumCompatibleVersion nao pode ser maior que a versao do projeto.' }
        $notes = @($Changelog | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() })
        if (-not $notes.Count -or ($notes -join "`n").Length -gt 10000) { throw 'Informe -Changelog com as alteracoes desta versao (ate 10.000 caracteres).' }
        $artifactPath = Join-Path $versionsPath $version
        if (Test-Path -LiteralPath $artifactPath) { throw "Os artefatos de $version ja existem em $artifactPath. Para reenvio use -UploadOnly -ArtifactDirectory; para novo conteudo incremente a versao." }
        $compiler = Resolve-InnoCompiler
        $runtime = Resolve-RuntimeInstaller
        if (-not $SkipServerUpload) {
            $session = Open-SigSession -ServerUrl $ServerUrl -Credential $Credential
            Assert-SigVersionAvailable -Session $session -Version $version
        }
        $stage = Join-Path $stagingRoot ([guid]::NewGuid().ToString('N'))
        $publishPath = Join-Path $stage 'publish'
        $outputPath = Join-Path $stage 'release'
        New-Item -ItemType Directory -Path $publishPath, $outputPath -Force | Out-Null
        Write-Host "Gerando Operacional $version para win-x64."
        Invoke-NativeCommand -FilePath 'dotnet' -Arguments @('publish', $projectFile, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', $publishPath)
        foreach ($file in @('Operacional.exe', 'Operacional.dll', 'Operacional.deps.json', 'Operacional.runtimeconfig.json', 'Update.exe', 'Update.dll', 'Update.deps.json', 'Update.runtimeconfig.json', 'BibliotecasSIG.dll')) {
            if (-not (Test-Path -LiteralPath (Join-Path $publishPath $file) -PathType Leaf)) { throw "Arquivo obrigatorio ausente no publish: $file" }
        }
        $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $publishPath 'Operacional.dll')).Version.ToString()
        if ($assemblyVersion -ne $version) { throw "A DLL gerada tem versao $assemblyVersion; era esperada $version." }
        # Local database settings must not be shipped or overwrite workstation settings.
        foreach ($configName in @('Operacional.dll.config', 'Operacional.exe.config', 'App.config')) {
            $configPath = Join-Path $publishPath $configName
            if (Test-Path -LiteralPath $configPath) { Remove-Item -LiteralPath $configPath -Force }
        }
        $innoArgs = @((Join-Path $projectPath 'Setup.iss'), "/DMyAppVersion=$version", "/DPublishSourcePath=$publishPath", "/DInstallerOutputPath=$outputPath")
        if ($runtime) { $innoArgs += "/DDotNetRuntimeInstaller=$runtime" }
        else { $innoArgs += '/DSkipRuntimeBundle=1' }
        Invoke-NativeCommand -FilePath $compiler -Arguments $innoArgs
        $zip = Join-Path $outputPath "application-$version.zip"
        [IO.Compression.ZipFile]::CreateFromDirectory($publishPath, $zip, [IO.Compression.CompressionLevel]::Optimal, $false)
        $installer = Join-Path $outputPath "OperacionalSetup-$version.exe"
        $zipFile = Get-Item -LiteralPath $zip
        $installerFile = Get-Item -LiteralPath $installer
        if ($zipFile.Length -gt 1000000000 -or $installerFile.Length -gt 500000000) { throw 'Artefatos excedem os limites da Central: ZIP 1 GB, EXE 500 MB.' }
        $metadata = [ordered]@{
            schemaVersion = 1; system = 'operacional'; version = $version; minimumCompatibleVersion = $minimum
            changelog = $notes; builtAt = [DateTimeOffset]::UtcNow.ToString('o'); target = 'win-x64'; runtimeBundled = [bool]$runtime
            package = @{ file = $zipFile.Name; size = $zipFile.Length; sha256 = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant() }
            installer = @{ file = $installerFile.Name; size = $installerFile.Length; sha256 = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant() }
        }
        $metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputPath 'release.json') -Encoding utf8NoBOM
        New-Item -ItemType Directory -Path $versionsPath -Force | Out-Null
        # Do not replace an existing version, including one produced by another process.
        New-Item -ItemType Directory -Path $artifactPath -ErrorAction Stop | Out-Null
        foreach ($file in Get-ChildItem -LiteralPath $outputPath -File) { Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $artifactPath $file.Name) }
        $release = Read-SigRelease -ArtifactDirectory $artifactPath
        Remove-StagingDirectory -Path $stage
        $stage = $null
        Write-Host "Artefatos preservados em: $artifactPath"
    }
    if (-not $SkipServerUpload) {
        if (-not $session) { $session = Open-SigSession -ServerUrl $ServerUrl -Credential $Credential }
        Assert-SigVersionAvailable -Session $session -Version $version
        $id = Send-SigRelease -Session $session -Release $release
        Write-Host "Rascunho enviado: $id"
        Write-Host "Abra $($session.BaseUrl), homologue a versao e depois publique em producao."
    } else { Write-Host 'Geracao local concluida. Nenhum arquivo enviado a Central ou ao compartilhamento.' }
    Write-Host "ZIP: $(Join-Path $release.Directory $release.Metadata.package.file)"
    Write-Host "Instalador: $(Join-Path $release.Directory $release.Metadata.installer.file)"
    Write-Host 'O version.json e gerado pela Central ao publicar. Nao envie um manifesto manual.'
} finally {
    Close-SigSession -Session $session
    if ($stage -and (Test-Path -LiteralPath $stage)) { Write-Warning "Arquivos de diagnostico preservados em: $stage" }
}
