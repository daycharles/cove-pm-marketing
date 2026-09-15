[CmdletBinding()]
param([switch]$NonInteractive)

$ErrorActionPreference = 'Stop'
$bundle = Split-Path -Parent $MyInvocation.MyCommand.Path
$deployer = Join-Path $bundle 'propflow-deploy.exe'
$compose = Join-Path $bundle 'compose.yaml'

function Say([string]$Message) { Write-Host "[PropFlow] $Message" }
function Fail([string]$Message) { Write-Error "[PropFlow] $Message"; exit 1 }
function Ask([string]$Message) {
    if ($NonInteractive) { return $true }
    $answer = Read-Host "$Message [Y/n]"
    return [string]::IsNullOrWhiteSpace($answer) -or $answer -match '^(y|yes)$'
}

if (-not (Test-Path $deployer)) { Fail "The installer is incomplete: propflow-deploy.exe is missing from $bundle." }
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { Fail 'Docker Desktop is required. Install it, start it, and run this installer again.' }
try { docker info *> $null } catch { Fail 'Docker Desktop is installed but is not running. Start it and try again.' }
if (-not (Get-Command node -ErrorAction SilentlyContinue)) { Fail 'Node.js 22 or newer is required. Install it and run this installer again.' }
$nodeVersion = (& node --version).Trim()
if ($nodeVersion -notmatch '^v(2[2-9]|[3-9][0-9])\.') { Fail "Node.js 22 or newer is required; found $nodeVersion." }

$state = $env:PROPFLOW_STATE
if ([string]::IsNullOrWhiteSpace($state)) { $state = Join-Path $env:ProgramData 'PropFlow' }
$stateExists = Test-Path (Join-Path $state 'deployment.json')
$mode = if ($stateExists) { 'upgrade' } else { 'new install' }

Say "Detected $mode."
Say "Bundle: $bundle"
Say "State:  $state"
Say 'The guided installer delegates stack changes to the trusted propflow-deploy executable.'
if (-not (Ask "Continue with this $mode?")) { Say 'Cancelled. No changes were made.'; exit 0 }

if ($stateExists) {
    $backup = Join-Path (Join-Path $state 'backups') ("pre-upgrade-" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Force -Path $backup | Out-Null
    Say "Creating a pre-upgrade snapshot in $backup..."
    Copy-Item (Join-Path $state 'deployment.json') $backup -Force
    foreach ($name in @('dataprotection-keys', 'attachments')) {
        $source = Join-Path $state $name
        if (Test-Path $source) { Copy-Item $source (Join-Path $backup $name) -Recurse -Force }
    }
    try {
        $secrets = Get-Content (Join-Path $state 'deployment.json') -Raw | ConvertFrom-Json
        $env:POSTGRES_PASSWORD = $secrets.postgresPassword
        Say 'Creating a database backup...'
        & docker compose --project-name propflow --file $compose exec -T database pg_dump -U propflow -d propflow -Fc > (Join-Path $backup 'propflow.dump')
        if ($LASTEXITCODE -ne 0) { throw 'pg_dump returned a non-zero exit code.' }
    } catch { Remove-Item Env:POSTGRES_PASSWORD -ErrorAction SilentlyContinue; Fail "The pre-upgrade backup failed. Nothing was upgraded. Details: $($_.Exception.Message)" }
    Remove-Item Env:POSTGRES_PASSWORD -ErrorAction SilentlyContinue
    Say 'Backup complete. Stopping the previous stack...'
    & $deployer --state $state down
    if ($LASTEXITCODE -ne 0) { Fail 'The previous stack could not be stopped safely.' }
}

Say 'Starting PropFlow and applying migrations, permissions, and health checks...'
& $deployer --state $state up
if ($LASTEXITCODE -ne 0) { Say "Startup failed. Inspect logs under $(Join-Path $state 'logs')."; exit $LASTEXITCODE }
Say 'Verifying the running workspace...'
& $deployer --state $state status
if ($LASTEXITCODE -ne 0) { Fail 'PropFlow started, but verification reported a failure.' }
try {
    $ready = Invoke-WebRequest -Uri 'https://localhost:5001/health/ready' -SkipCertificateCheck -TimeoutSec 10
    if ($ready.StatusCode -ne 200) { Fail "The API readiness check returned HTTP $($ready.StatusCode)." }
} catch { Fail "The API readiness check failed: $($_.Exception.Message)" }
Say 'Installation complete. Open http://127.0.0.1:3000 to finish setup.'
