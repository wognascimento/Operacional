#Requires -Version 7.0
Set-StrictMode -Version Latest

function ConvertTo-SigVersion {
    param([Parameter(Mandatory)][string]$Value)
    if ($Value -notmatch '^\d{1,5}\.\d{1,5}\.\d{1,5}(\.\d{1,5})?$') { throw "Versao invalida: '$Value'. Use 2.0.0.1." }
    $v = [version]$Value
    foreach ($part in @($v.Major, $v.Minor, $v.Build, [Math]::Max(0, $v.Revision))) {
        if ($part -gt 65534) { throw 'Cada componente da versao deve estar entre 0 e 65534.' }
    }
    return [version]::new($v.Major, $v.Minor, $v.Build, [Math]::Max(0, $v.Revision)).ToString()
}

function Get-SigServerUrl {
    param([Parameter(Mandatory)][string]$ServerUrl)
    $uri = $null
    if (-not [uri]::TryCreate($ServerUrl, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne 'https' -or $uri.UserInfo -or $uri.Query -or $uri.Fragment -or $uri.AbsolutePath -ne '/') {
        throw 'ServerUrl deve ser a origem HTTPS da Central, sem usuario, senha, caminho ou parametros.'
    }
    return $uri.GetLeftPart([UriPartial]::Authority)
}

function Open-SigSession {
    param([Parameter(Mandatory)][string]$ServerUrl, [PSCredential]$Credential)
    $baseUrl = Get-SigServerUrl $ServerUrl
    $info = Invoke-RestMethod "$baseUrl/api/session" -SessionVariable webSession -TimeoutSec 30 -MaximumRedirection 0
    if ($info.needsSetup) { throw "Crie primeiro sua conta em $baseUrl usando o codigo de ativacao." }
    if (-not $Credential) { $Credential = Get-Credential -Message 'Conta administrativa da Central de Atualizacoes SIG (nao e a conta SSH)' }
    if (-not $Credential) { throw 'Autenticacao cancelada.' }
    $session = [pscustomobject]@{ BaseUrl = $baseUrl; WebSession = $webSession; Headers = @{ 'X-CSRF-TOKEN' = $info.csrf }; LoggedIn = $false }
    $loginJson = $null
    try {
        $loginJson = @{ username = $Credential.UserName; password = $Credential.GetNetworkCredential().Password } | ConvertTo-Json -Compress
        Invoke-RestMethod "$baseUrl/api/login" -Method Post -WebSession $webSession -Headers $session.Headers -ContentType 'application/json' -Body $loginJson -TimeoutSec 30 -MaximumRedirection 0 | Out-Null
        $session.LoggedIn = $true
        $info = Invoke-RestMethod "$baseUrl/api/session" -WebSession $webSession -TimeoutSec 30 -MaximumRedirection 0
        $session.Headers['X-CSRF-TOKEN'] = $info.csrf
        if (-not $info.authenticated) { throw 'A Central nao confirmou a autenticacao.' }
        return $session
    } catch {
        if ($session.LoggedIn) { Close-SigSession $session }
        throw "Nao foi possivel entrar na Central. Confira usuario, senha e acesso HTTPS. $($_.Exception.Message)"
    } finally { $loginJson = $null }
}

function Close-SigSession {
    param($Session)
    if ($null -eq $Session -or -not $Session.LoggedIn) { return }
    try {
        # Refresh the antiforgery token even if an earlier operation failed.
        $info = Invoke-RestMethod "$($Session.BaseUrl)/api/session" -WebSession $Session.WebSession -TimeoutSec 30 -MaximumRedirection 0
        $Session.Headers['X-CSRF-TOKEN'] = $info.csrf
        Invoke-RestMethod "$($Session.BaseUrl)/api/logout" -Method Post -WebSession $Session.WebSession -Headers $Session.Headers -TimeoutSec 30 -MaximumRedirection 0 | Out-Null
        $Session.LoggedIn = $false
    } catch { Write-Warning 'Nao foi possivel encerrar a sessao da Central. Ela expira automaticamente em ate 8 horas.' }
}

function Assert-SigVersionAvailable {
    param([Parameter(Mandatory)]$Session, [Parameter(Mandatory)][string]$Version)
    $v = ConvertTo-SigVersion $Version
    $dashboard = Invoke-RestMethod "$($Session.BaseUrl)/api/dashboard" -WebSession $Session.WebSession -TimeoutSec 30 -MaximumRedirection 0
    if (-not @($dashboard.systems | Where-Object id -eq 'operacional').Count) { throw 'Cadastre o sistema operacional na Central antes de enviar.' }
    $releases = @($dashboard.releases | Where-Object systemId -eq 'operacional')
    foreach ($release in $releases) {
        if ((ConvertTo-SigVersion $release.version) -eq $v) { throw "A versao $v ja existe na Central (inclusive rascunhos/suspensas). Nao pode ser sobrescrita; incremente a versao." }
        if ($release.channels -contains 'producao' -and [version]$v -le [version]$release.version) { throw "A versao $v deve ser maior que a versao em producao ($($release.version))." }
    }
}

function Read-SigRelease {
    param([Parameter(Mandatory)][string]$ArtifactDirectory)
    $directory = (Resolve-Path -LiteralPath $ArtifactDirectory -ErrorAction Stop).Path
    $release = Get-Content -LiteralPath (Join-Path $directory 'release.json') -Raw | ConvertFrom-Json
    if ($release.schemaVersion -ne 1 -or $release.system -ne 'operacional') { throw 'release.json nao pertence ao formato de publicacao do Operacional.' }
    $v = ConvertTo-SigVersion $release.version
    if ($release.version -ne $v) { throw 'A versao do release.json deve conter quatro componentes.' }
    $minimum = ConvertTo-SigVersion $release.minimumCompatibleVersion
    if ([version]$minimum -gt [version]$v) { throw 'A versao minima e maior que a versao do pacote.' }
    if ($release.package.file -ne "application-$v.zip" -or $release.installer.file -ne "OperacionalSetup-$v.exe") { throw 'Nomes de artefatos inesperados em release.json.' }
    if (-not @($release.changelog).Count -or [string]::IsNullOrWhiteSpace(($release.changelog -join "`n"))) { throw 'Historico de alteracoes vazio.' }
    foreach ($artifact in @($release.package, $release.installer)) {
        $file = Get-Item -LiteralPath (Join-Path $directory $artifact.file) -ErrorAction Stop
        if ($file.Length -ne $artifact.size -or (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne $artifact.sha256) {
            throw "O arquivo $($artifact.file) mudou ou esta incompleto. O hash/tamanho difere de release.json."
        }
    }
    return [pscustomobject]@{ Directory = $directory; Metadata = $release }
}

function Send-SigRelease {
    param([Parameter(Mandatory)]$Session, [Parameter(Mandatory)]$Release)
    $r = $Release.Metadata
    $form = @{
        system = 'operacional'; version = $r.version; minimumCompatibleVersion = $r.minimumCompatibleVersion
        changelog = ($r.changelog -join "`n")
        package = Get-Item -LiteralPath (Join-Path $Release.Directory $r.package.file)
        installer = Get-Item -LiteralPath (Join-Path $Release.Directory $r.installer.file)
    }
    try {
        $result = Invoke-RestMethod "$($Session.BaseUrl)/api/releases" -Method Post -WebSession $Session.WebSession -Headers $Session.Headers -Form $form -TimeoutSec 1800 -MaximumRedirection 0
    } catch {
        # An interrupted response may follow a committed upload. Never overwrite or auto-retry blindly.
        throw "Falha ao confirmar o envio. Os artefatos locais foram preservados. Confira se a versao ja aparece na Central antes de tentar -UploadOnly. $($_.Exception.Message)"
    }
    if ([string]::IsNullOrWhiteSpace($result.id)) { throw 'A Central nao retornou o identificador do rascunho. Confira o painel antes de repetir.' }
    $receipt = @{ releaseId = $result.id; version = $r.version; server = $Session.BaseUrl; uploadedAt = [DateTimeOffset]::UtcNow.ToString('o'); state = 'rascunho' }
    $receipt | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Release.Directory 'publication.json') -Encoding utf8NoBOM
    return $result.id
}

function Copy-SigPublishedInstaller {
    param([Parameter(Mandatory)]$Release, [Parameter(Mandatory)][string]$ServerUrl, [Parameter(Mandatory)][string]$Destination, [PSCredential]$Credential)
    $baseUrl = Get-SigServerUrl $ServerUrl
    $manifest = Invoke-RestMethod "$baseUrl/downloads/operacional/version.json" -TimeoutSec 30 -MaximumRedirection 0
    $r = $Release.Metadata
    if ($manifest.updateVersion -ne $r.version -or $manifest.installerSha256 -ne $r.installer.sha256) { throw 'Copia bloqueada: a versao/instalador local ainda nao corresponde ao publicado em producao na Central.' }
    $driveName = $null
    try {
        $target = $Destination.TrimEnd('\')
        if ($Credential) {
            if ($target -notmatch '^\\\\([^\\]+)\\([^\\]+)(\\.*)?$') { throw 'NetworkCredential exige um caminho UNC.' }
            $share = "\\$($Matches[1])\$($Matches[2])"
            $relative = ([string]$Matches[3]).TrimStart('\')
            $driveName = 'SIGDEPLOY' + [guid]::NewGuid().ToString('N')
            New-PSDrive -Name $driveName -PSProvider FileSystem -Root $share -Credential $Credential -ErrorAction Stop | Out-Null
            $target = if ($relative) { Join-Path "${driveName}:\" $relative } else { "${driveName}:\" }
        }
        if (-not (Test-Path -LiteralPath $target -PathType Container)) { throw "Compartilhamento/diretorio inexistente: $Destination" }
        $source = Join-Path $Release.Directory $r.installer.file
        $destinationFile = Join-Path $target $r.installer.file
        if (Test-Path -LiteralPath $destinationFile) {
            if ((Get-FileHash -LiteralPath $destinationFile -Algorithm SHA256).Hash -ne $r.installer.sha256) { throw 'Ja existe um instalador diferente com o mesmo nome no destino. A copia nao foi sobrescrita.' }
        } else { Copy-Item -LiteralPath $source -Destination $destinationFile -ErrorAction Stop }
        Write-Host "Instalador publicado copiado para: $Destination"
    } finally { if ($driveName) { Remove-PSDrive -Name $driveName -Force -ErrorAction SilentlyContinue } }
}

Export-ModuleMember -Function ConvertTo-SigVersion, Get-SigServerUrl, Open-SigSession, Close-SigSession, Assert-SigVersionAvailable, Read-SigRelease, Send-SigRelease, Copy-SigPublishedInstaller
