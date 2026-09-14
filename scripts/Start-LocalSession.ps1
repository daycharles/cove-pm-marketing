param(
    [int]$PostgresPort = 5432,
    [int]$ApiPort = 5001,
    [int]$WebPort = 3000,
    [ValidateSet('http', 'https')][string]$ApiScheme = 'https',
    [switch]$WebOnly,
    [switch]$ApiOnly,
    [string]$ApiLog = 'tmp/local-api.stdout.log',
    [string]$ApiErrorLog = 'tmp/local-api.stderr.log',
    [string]$WebLog = 'tmp/local-web.stdout.log',
    [string]$WebErrorLog = 'tmp/local-web.stderr.log',
    [string]$StatePath = '',
    [string]$NextDistDir = '',
    [string]$ApiDllPath = ''
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
foreach ($line in Get-Content (Join-Path $root '.env')) {
    if ($line -match '^([^#=]+)=(.*)$') { Set-Item "Env:$($Matches[1])" $Matches[2] }
}
$env:ConnectionStrings__Database = "Host=localhost;Port=$PostgresPort;Database=$env:POSTGRES_DB;Username=propflow_app;Password=$env:APP_DB_PASSWORD"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = "${ApiScheme}://localhost:$ApiPort"
if ($StatePath) {
    $resolvedState = [IO.Path]::GetFullPath((Join-Path $root $StatePath))
    New-Item -ItemType Directory -Force -Path $resolvedState | Out-Null
    $env:DataProtection__KeyPath = $resolvedState
}
if (-not $WebOnly) {
    $apiArguments = if ($ApiDllPath) { @($ApiDllPath) } else { @('run','--project','src/PropFlow.Api','--configuration','Release','--no-build') }
    Start-Process dotnet -WindowStyle Hidden -WorkingDirectory $root -ArgumentList $apiArguments -RedirectStandardOutput (Join-Path $root $ApiLog) -RedirectStandardError (Join-Path $root $ApiErrorLog)
}
$env:PROPFLOW_API_ORIGIN = "${ApiScheme}://localhost:$ApiPort"
$webRoot = Join-Path $root 'apps/web'
$env:NEXT_DIST_DIR = if ($NextDistDir) {
    if ([IO.Path]::IsPathRooted($NextDistDir) -or $NextDistDir.Contains('..')) { throw 'NextDistDir must remain inside apps/web.' }
    $resolvedNextDist = [IO.Path]::GetFullPath((Join-Path $webRoot $NextDistDir))
    New-Item -ItemType Directory -Force -Path $resolvedNextDist | Out-Null
    $NextDistDir.Replace('\', '/')
} else { Remove-Item Env:NEXT_DIST_DIR -ErrorAction SilentlyContinue; $null }
$env:NODE_TLS_REJECT_UNAUTHORIZED = '0'
$env:NODE_OPTIONS = '--use-system-ca'
if (-not $ApiOnly) {
    Start-Process npm.cmd -WindowStyle Hidden -WorkingDirectory (Join-Path $root 'apps/web') -ArgumentList 'run','dev','--','--port',"$WebPort" -RedirectStandardOutput (Join-Path $root $WebLog) -RedirectStandardError (Join-Path $root $WebErrorLog)
}
Write-Output "Started Cove session: PostgreSQL=$PostgresPort API=$ApiPort Web=$WebPort"
