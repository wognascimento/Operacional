param(
    [string]$ServerUploadPath = "root@192.168.0.49:/var/www/updates/downloads/operacional/",
    [string]$UpdateBaseUrl = "http://192.168.0.49/downloads/operacional",
    [string]$NetworkDeployPath = "\\192.168.0.4\sistemas\SIG",
    [string]$InnoCompiler = "",
    [string]$DotNetDesktopRuntimeInstallerPath = "",
    [System.Management.Automation.PSCredential]$NetworkCredential = $null,
    [string[]]$Changelog = @("Ajustes", "Melhorias"),
    [switch]$SkipServerUpload,
    [switch]$SkipNetworkCopy,
    [switch]$ForceDeploy
)

$ErrorActionPreference = "Stop"

$projectPath = $PSScriptRoot
$workspaceRoot = Resolve-Path (Join-Path $projectPath "..")
$projectFile = Join-Path $projectPath "Operacional\Operacional.csproj"
$publishPath = Join-Path $projectPath "publish"
$artifactsPath = Join-Path $projectPath "artifacts"
$installerPath = Join-Path $artifactsPath "installer"
$versionJsonPath = Join-Path $artifactsPath "version.json"
$redistPath = Join-Path $projectPath "redist"
$runtimeInstallerName = "windowsdesktop-runtime-10.0-win-x64.exe"
$runtimeInstallerSearchPattern = "windowsdesktop-runtime-10.*-win-x64.exe"
$runtimeInstallerTargetPath = Join-Path $redistPath $runtimeInstallerName

function Get-ApplicationVersion {
    $projContent = [xml](Get-Content $projectFile)
    $propertyGroups = @($projContent.Project.PropertyGroup)

    $version = ($propertyGroups | ForEach-Object { $_.AssemblyVersion } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)

    if ([string]::IsNullOrWhiteSpace($version)) {
        $version = ($propertyGroups | ForEach-Object { $_.Version } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
    }

    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "Versao nao encontrada no .csproj. Informe <AssemblyVersion> ou <Version>."
    }

    return $version
}

function Resolve-InnoCompiler {
    param([string]$ConfiguredPath)

    $candidates = @()

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
        $candidates += $ConfiguredPath
    }

    $candidates += Join-Path $workspaceRoot "tools\InnoSetup6\ISCC.exe"
    $candidates += "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "ISCC.exe nao encontrado. Coloque o Inno Setup portable em '$workspaceRoot\tools\InnoSetup6' ou informe -InnoCompiler."
}

function Resolve-DotNetRuntimeInstaller {
    param([string]$ConfiguredPath)

    $candidates = @()

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
        $candidates += $ConfiguredPath
    }

    $candidates += Join-Path $workspaceRoot "tools\dotnet\$runtimeInstallerName"

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    $runtimeDirectory = Join-Path $workspaceRoot "tools\dotnet"
    if (Test-Path $runtimeDirectory) {
        $installer = Get-ChildItem -Path $runtimeDirectory -Filter $runtimeInstallerSearchPattern -File |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if ($installer) {
            return $installer.FullName
        }
    }

    return ""
}

function Invoke-NativeCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Comando falhou com codigo ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Copy-ToNetwork {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [System.Management.Automation.PSCredential]$Credential = $null
    )

    if ([string]::IsNullOrWhiteSpace($DestinationPath)) {
        return
    }

    $destination = $DestinationPath.Trim().TrimEnd("\")
    $copyDestination = $destination
    $temporaryDriveName = $null

    try {
        if ($Credential -and $destination -match "^\\\\([^\\]+)\\([^\\]+)(\\.*)?$") {
            $networkShare = "\\$($Matches[1])\$($Matches[2])"
            $relativePath = $Matches[3]

            if ([string]::IsNullOrWhiteSpace($relativePath)) {
                $relativePath = ""
            }
            else {
                $relativePath = $relativePath.TrimStart("\")
            }

            $temporaryDriveName = "SIGDEPLOY$([Guid]::NewGuid().ToString("N").Substring(0, 8))"
            New-PSDrive -Name $temporaryDriveName -PSProvider FileSystem -Root $networkShare -Credential $Credential -ErrorAction Stop | Out-Null

            if ([string]::IsNullOrWhiteSpace($relativePath)) {
                $copyDestination = "${temporaryDriveName}:\"
            }
            else {
                $copyDestination = Join-Path "${temporaryDriveName}:\" $relativePath
            }
        }

        if (-not (Test-Path -LiteralPath $copyDestination -PathType Container)) {
            New-Item -ItemType Directory -Path $copyDestination -Force | Out-Null
        }

        Copy-Item -LiteralPath $SourcePath -Destination $copyDestination -Force
        Write-Host "Instalador copiado para: $destination"
    }
    catch {
        throw "Nao foi possivel copiar o instalador para '$destination'. Verifique se o compartilhamento existe, se o usuario tem permissao e, se necessario, execute com -NetworkCredential. Detalhe: $($_.Exception.Message)"
    }
    finally {
        if ($temporaryDriveName) {
            Remove-PSDrive -Name $temporaryDriveName -Force -ErrorAction SilentlyContinue
        }
    }
}

function Convert-ToVersion {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Versao vazia."
    }

    try {
        return [version]$Value
    }
    catch {
        throw "Versao invalida: $Value"
    }
}

function Convert-BytesToText {
    param([byte[]]$Bytes)

    if ($Bytes.Length -ge 2 -and $Bytes[0] -eq 0xFF -and $Bytes[1] -eq 0xFE) {
        return [System.Text.Encoding]::Unicode.GetString($Bytes, 2, $Bytes.Length - 2)
    }

    if ($Bytes.Length -ge 2 -and $Bytes[0] -eq 0xFE -and $Bytes[1] -eq 0xFF) {
        return [System.Text.Encoding]::BigEndianUnicode.GetString($Bytes, 2, $Bytes.Length - 2)
    }

    if ($Bytes.Length -ge 3 -and $Bytes[0] -eq 0xEF -and $Bytes[1] -eq 0xBB -and $Bytes[2] -eq 0xBF) {
        return [System.Text.Encoding]::UTF8.GetString($Bytes, 3, $Bytes.Length - 3)
    }

    return [System.Text.Encoding]::UTF8.GetString($Bytes)
}

function Test-ServerVersion {
    param(
        [string]$LocalVersion,
        [string]$BaseUrl,
        [switch]$Force
    )

    if ($Force) {
        Write-Host "Validacao de versao do servidor ignorada por -ForceDeploy."
        return
    }

    $versionUrl = "$($BaseUrl.TrimEnd('/'))/version.json"
    Write-Host "Verificando versao publicada: $versionUrl"

    try {
        $webClient = [System.Net.WebClient]::new()
        $versionBytes = $webClient.DownloadData($versionUrl)
    }
    catch {
        $statusCode = $null
        $exception = $_.Exception

        while ($exception) {
            if ($exception.Response -and $exception.Response.StatusCode) {
                $statusCode = [int]$exception.Response.StatusCode
                break
            }

            $exception = $exception.InnerException
        }

        if ($statusCode -eq 404 -or $_.Exception.Message -match "\(404\)|404|Nao Localizado|Não Localizado|Not Found") {
            Write-Host "Nenhum version.json encontrado no servidor. Deploy inicial permitido."
            return
        }

        throw "Nao foi possivel consultar a versao atual no servidor: $($_.Exception.Message)"
    }
    finally {
        if ($webClient) {
            $webClient.Dispose()
        }
    }

    try {
        $serverJson = Convert-BytesToText -Bytes $versionBytes
        $serverInfo = $serverJson | ConvertFrom-Json
    }
    catch {
        throw "O version.json do servidor nao esta em um formato JSON valido. Arquivo consultado: $versionUrl. Detalhe: $($_.Exception.Message)"
    }

    $serverVersionText = $serverInfo.updateVersion

    if ([string]::IsNullOrWhiteSpace($serverVersionText)) {
        $serverVersionText = $serverInfo.currentVersion
    }

    if ([string]::IsNullOrWhiteSpace($serverVersionText)) {
        throw "O version.json do servidor nao contem updateVersion nem currentVersion."
    }

    $local = Convert-ToVersion -Value $LocalVersion
    $server = Convert-ToVersion -Value $serverVersionText

    Write-Host "Versao local: $local"
    Write-Host "Versao no servidor: $server"

    if ($local -le $server) {
        throw "Deploy bloqueado. A versao local ($local) e igual ou inferior a versao publicada ($server). Atualize a versao do projeto ou use -ForceDeploy."
    }
}

$version = Get-ApplicationVersion
$resolvedInnoCompiler = Resolve-InnoCompiler -ConfiguredPath $InnoCompiler
$runtimeInstallerSourcePath = Resolve-DotNetRuntimeInstaller -ConfiguredPath $DotNetDesktopRuntimeInstallerPath
$zipFileName = "application-$version.zip"
$zipFullPath = Join-Path $artifactsPath $zipFileName

Write-Host "Projeto: $projectPath"
Write-Host "Versao: $version"
Write-Host "Inno Setup: $resolvedInnoCompiler"

if ([string]::IsNullOrWhiteSpace($runtimeInstallerSourcePath)) {
    Write-Host ".NET Desktop Runtime 10 nao sera embutido. Coloque '$runtimeInstallerName' ou '$runtimeInstallerSearchPattern' em '$workspaceRoot\tools\dotnet' ou informe -DotNetDesktopRuntimeInstallerPath."
}
else {
    Write-Host ".NET Desktop Runtime 10: $runtimeInstallerSourcePath"
}

if (Test-Path $publishPath) {
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}

if (Test-Path $artifactsPath) {
    Remove-Item -LiteralPath $artifactsPath -Recurse -Force
}

if (Test-Path $redistPath) {
    Remove-Item -LiteralPath $redistPath -Recurse -Force
}

New-Item -ItemType Directory -Path $publishPath -Force | Out-Null
New-Item -ItemType Directory -Path $installerPath -Force | Out-Null
New-Item -ItemType Directory -Path $redistPath -Force | Out-Null

if (-not [string]::IsNullOrWhiteSpace($runtimeInstallerSourcePath)) {
    Copy-Item -Path $runtimeInstallerSourcePath -Destination $runtimeInstallerTargetPath -Force
}

Invoke-NativeCommand -FilePath "dotnet" -Arguments @("publish", $projectFile, "-c", "Release", "-o", $publishPath)

Push-Location $projectPath
try {
    Invoke-NativeCommand -FilePath $resolvedInnoCompiler -Arguments @("Setup.iss", "/DMyAppVersion=$version")
}
finally {
    Pop-Location
}

Compress-Archive -Path (Join-Path $publishPath "*") -DestinationPath $zipFullPath -Force

$updateJson = @{
    currentVersion = $version
    updateVersion = $version
    updateUrl = "$($UpdateBaseUrl.TrimEnd('/'))/$zipFileName"
    changelog = $Changelog
    releaseDate = (Get-Date).ToString("yyyy-MM-dd")
    minimumCompatibleVersion = "1.0.0"
} | ConvertTo-Json

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($versionJsonPath, $updateJson, $utf8NoBom)

$installerFile = Get-ChildItem -Path $installerPath -Filter "OperacionalSetup-$version.exe" | Select-Object -First 1

if (-not $installerFile) {
    throw "Instalador nao foi gerado em $installerPath"
}

if (-not $SkipServerUpload) {
    Test-ServerVersion -LocalVersion $version -BaseUrl $UpdateBaseUrl -Force:$ForceDeploy

    $remoteBasePath = $ServerUploadPath.TrimEnd("/")
    $tempZipRemotePath = "$remoteBasePath/$zipFileName.tmp"
    $remoteHost = $remoteBasePath.Split(":")[0]
    $remoteDirectory = $remoteBasePath.Split(":")[1]

    Invoke-NativeCommand -FilePath "scp" -Arguments @($zipFullPath, $tempZipRemotePath)
    Invoke-NativeCommand -FilePath "ssh" -Arguments @($remoteHost, "mv '$remoteDirectory/$zipFileName.tmp' '$remoteDirectory/$zipFileName'")
    Invoke-NativeCommand -FilePath "scp" -Arguments @($versionJsonPath, "$remoteBasePath/version.json")
}

if (-not $SkipNetworkCopy) {
    Copy-ToNetwork -SourcePath $installerFile.FullName -DestinationPath $NetworkDeployPath -Credential $NetworkCredential
}

Write-Host "Deploy concluido para versao $version"
Write-Host "Pacote de atualizacao: $zipFullPath"
Write-Host "JSON de atualizacao: $versionJsonPath"
Write-Host "Instalador: $($installerFile.FullName)"
