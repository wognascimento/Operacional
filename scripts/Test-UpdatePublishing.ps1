#Requires -Version 7.0
# Contract tests: no connection to the real Central and no publication.
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'UpdatePublishing.psm1') -Force
$module = Get-Module UpdatePublishing
$script:checks = 0
function Assert-True($Condition, $Message) { if (-not $Condition) { throw $Message }; $script:checks++ }
function Assert-Throws([scriptblock]$Action, [string]$MessagePart) {
    try { & $Action; throw 'EXPECTED_FAILURE_MISSING' } catch {
        if ($_.Exception.Message -eq 'EXPECTED_FAILURE_MISSING' -or $_.Exception.Message -notlike "*$MessagePart*") { throw }
        $script:checks++
    }
}
Assert-True ((ConvertTo-SigVersion '2.0.1') -eq '2.0.1.0') 'Version normalization failed.'
Assert-Throws { ConvertTo-SigVersion '2.0.beta' } 'Versao invalida'
Assert-Throws { ConvertTo-SigVersion '70000.0.0.0' } '65534'
Assert-Throws { Get-SigServerUrl 'http://example.test' } 'HTTPS'
Assert-Throws { Get-SigServerUrl 'https://user:password@example.test' } 'HTTPS'
Assert-Throws { Get-SigServerUrl 'https://example.test/path' } 'HTTPS'

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\artifacts\.tests'))
$fixture = Join-Path $root ([guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
try {
    [IO.File]::WriteAllText((Join-Path $fixture 'application-2.0.1.0.zip'), 'test-package')
    [IO.File]::WriteAllText((Join-Path $fixture 'OperacionalSetup-2.0.1.0.exe'), 'test-installer')
    $metadata = @{
        schemaVersion = 1; system = 'operacional'; version = '2.0.1.0'; minimumCompatibleVersion = '1.0.0.0'; changelog = @('Teste de contrato')
        package = @{ file = 'application-2.0.1.0.zip'; size = 12; sha256 = (Get-FileHash (Join-Path $fixture 'application-2.0.1.0.zip')).Hash }
        installer = @{ file = 'OperacionalSetup-2.0.1.0.exe'; size = 14; sha256 = (Get-FileHash (Join-Path $fixture 'OperacionalSetup-2.0.1.0.exe')).Hash }
    }
    $metadata | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $fixture 'release.json')
    $release = Read-SigRelease $fixture
    Assert-True ($release.Metadata.version -eq '2.0.1.0') 'Metadata not loaded.'
    [IO.File]::WriteAllText((Join-Path $fixture 'application-2.0.1.0.zip'), 'altered-file')
    Assert-Throws { Read-SigRelease $fixture } 'hash/tamanho'
    [IO.File]::WriteAllText((Join-Path $fixture 'application-2.0.1.0.zip'), 'test-package')
    $metadata.package.file = '..\outside.zip'
    $metadata | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $fixture 'release.json')
    Assert-Throws { Read-SigRelease $fixture } 'Nomes de artefatos'
    $metadata.package.file = 'application-2.0.1.0.zip'
    $metadata | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $fixture 'release.json')

    & $module {
        $script:mockLoggedIn = $false
        $script:mockReleases = @()
        $script:mockUploadFailure = $false
        $script:mockForm = $null
        function script:Invoke-RestMethod {
            param($Uri, $Method = 'Get', $SessionVariable, $WebSession, $Headers, $ContentType, $Body, $Form, $TimeoutSec, $MaximumRedirection)
            if ($Uri -notlike 'https://example.test/*') { throw 'Unexpected destination.' }
            if ($MaximumRedirection -ne 0) { throw 'Redirects must be disabled for authenticated requests.' }
            if ($Uri.EndsWith('/api/session')) {
                if ($SessionVariable) { Set-Variable -Name $SessionVariable -Value 'test-session' -Scope 1 }
                return @{ authenticated = $script:mockLoggedIn; needsSetup = $false; csrf = if ($script:mockLoggedIn) { 'authenticated-token' } else { 'anonymous-token' } }
            }
            if ($Uri.EndsWith('/api/login')) {
                if ($Method -ne 'Post' -or $Headers['X-CSRF-TOKEN'] -ne 'anonymous-token') { throw 'Invalid login CSRF/method.' }
                $script:mockLoggedIn = $true
                return @{}
            }
            if (-not $script:mockLoggedIn) { throw 'Unauthenticated request.' }
            if ($Uri.EndsWith('/api/dashboard')) { return @{ systems = @(@{id='operacional'}); releases = $script:mockReleases } }
            if ($Headers['X-CSRF-TOKEN'] -ne 'authenticated-token') { throw 'CSRF token was not refreshed after login.' }
            if ($Uri.EndsWith('/api/releases')) {
                if ($Method -ne 'Post') { throw 'Invalid upload method.' }
                if ($script:mockUploadFailure) { throw 'Connection interrupted.' }
                $script:mockForm = $Form
                return @{id='test-release-id'}
            }
            if ($Uri.EndsWith('/api/logout')) { $script:mockLoggedIn = $false; return @{} }
            throw 'Unexpected endpoint.'
        }
    }
    $credential = [PSCredential]::new('testadmin', (ConvertTo-SecureString 'Test123!' -AsPlainText -Force))
    $session = Open-SigSession -ServerUrl 'https://example.test' -Credential $credential
    Assert-True ($session.Headers['X-CSRF-TOKEN'] -eq 'authenticated-token') 'CSRF token not renewed.'
    Assert-SigVersionAvailable $session '2.0.1.0'
    $script:checks++
    & $module { $script:mockReleases = @(@{systemId='operacional';version='2.0.1.0';channels=@();suspended=$true}) }
    Assert-Throws { Assert-SigVersionAvailable $session '2.0.1.0' } 'ja existe'
    & $module { $script:mockReleases = @(@{systemId='operacional';version='3.0.0.0';channels=@('producao')}) }
    Assert-Throws { Assert-SigVersionAvailable $session '2.0.1.0' } 'maior que'
    & $module { $script:mockReleases = @() }
    $id = Send-SigRelease $session $release
    Assert-True ($id -eq 'test-release-id') 'Release ID not returned.'
    $form = & $module { $script:mockForm }
    Assert-True ($form.system -eq 'operacional' -and $form.version -eq '2.0.1.0' -and $form.package -is [IO.FileInfo] -and $form.installer -is [IO.FileInfo]) 'Multipart contract invalid.'
    $receipt = Get-Content (Join-Path $fixture 'publication.json') -Raw
    Assert-True ($receipt -notmatch 'Test123|testadmin' -and $receipt -match 'test-release-id') 'Receipt contains credentials or has no ID.'
    & $module { $script:mockUploadFailure = $true }
    Assert-Throws { Send-SigRelease $session $release } 'Confira se a versao ja aparece'
    Close-SigSession $session
    Assert-True (-not $session.LoggedIn) 'Session not closed.'
    Write-Output "PASS: $script:checks publication checks. No real requests sent."
} finally {
    Remove-Module UpdatePublishing -Force
    $resolved = [IO.Path]::GetFullPath($fixture)
    if ($resolved.StartsWith($root.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
