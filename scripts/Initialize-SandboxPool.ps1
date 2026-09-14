param([switch]$SkipStart)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$runtime = Join-Path $root '.sandbox'
$registryPath = Join-Path $runtime 'registry.json'
New-Item -ItemType Directory -Force -Path $runtime | Out-Null

$slots = @(
    [ordered]@{ id = 'sandbox-1'; status = 'available'; postgresPort = 15432; apiPort = 15003; webPort = 13001; container = 'cove-sandbox-1-db'; volume = 'cove-sandbox-1-data' },
    [ordered]@{ id = 'sandbox-2'; status = 'available'; postgresPort = 15433; apiPort = 15004; webPort = 13002; container = 'cove-sandbox-2-db'; volume = 'cove-sandbox-2-data' },
    [ordered]@{ id = 'sandbox-3'; status = 'available'; postgresPort = 15434; apiPort = 15005; webPort = 13003; container = 'cove-sandbox-3-db'; volume = 'cove-sandbox-3-data' }
)
$slots | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $registryPath -Encoding utf8

if (-not $SkipStart) {
    $values = @{}
    foreach ($line in Get-Content (Join-Path $root '.env')) { if ($line -match '^([A-Z_]+)=(.*)$') { $values[$Matches[1]] = $Matches[2] } }
    foreach ($key in @('POSTGRES_DB','POSTGRES_USER','POSTGRES_PASSWORD','APP_DB_PASSWORD')) { if (-not $values[$key]) { throw "Missing $key in .env." } }
    $demoPassword = if ($values.DEMO_PASSWORD) { $values.DEMO_PASSWORD } else { 'SandboxDemo!2026' }
    if ($demoPassword.Length -lt 12) { throw 'DEMO_PASSWORD must be at least 12 characters.' }
    foreach ($slot in $slots) {
        $exists = docker ps -a --filter "name=^$($slot.container)$" --format '{{.Names}}'
        if (-not $exists) {
            docker volume create $slot.volume | Out-Null
            docker run -d --name $slot.container -e "POSTGRES_DB=$($values.POSTGRES_DB)" -e "POSTGRES_USER=$($values.POSTGRES_USER)" -e "POSTGRES_PASSWORD=$($values.POSTGRES_PASSWORD)" -p "127.0.0.1:$($slot.postgresPort):5432" -v "$($slot.volume):/var/lib/postgresql/data" postgres:17 | Out-Null
        } else { docker start $slot.container 2>$null | Out-Null }
        $env:ConnectionStrings__Admin = "Host=localhost;Port=$($slot.postgresPort);Database=$($values.POSTGRES_DB);Username=$($values.POSTGRES_USER);Password=$($values.POSTGRES_PASSWORD)"
        $env:Runtime__Password = $values.APP_DB_PASSWORD
        dotnet run --project (Join-Path $root 'tools/PropFlow.Admin') --configuration Release --no-build -- migrate
        if ($LASTEXITCODE -ne 0) { throw "Migration failed for $($slot.id)." }
        dotnet run --project (Join-Path $root 'tools/PropFlow.Admin') --configuration Release --no-build -- configure-runtime
        if ($LASTEXITCODE -ne 0) { throw "Runtime configuration failed for $($slot.id)." }
        $env:Demo__Password = $demoPassword
        dotnet run --project (Join-Path $root 'tools/PropFlow.Admin') --configuration Release --no-build -- seed-demo
        if ($LASTEXITCODE -ne 0) { throw "Demo seed failed for $($slot.id)." }
        Remove-Item Env:ConnectionStrings__Admin,Env:Runtime__Password,Env:Demo__Password -ErrorAction SilentlyContinue
        & (Join-Path $root 'scripts/Start-LocalSession.ps1') -PostgresPort $slot.postgresPort -ApiPort $slot.apiPort -WebPort $slot.webPort -StatePath ".sandbox/$($slot.id)/keys" -ApiLog "tmp/$($slot.id)-api.stdout.log" -ApiErrorLog "tmp/$($slot.id)-api.stderr.log" -WebLog "tmp/$($slot.id)-web.stdout.log" -WebErrorLog "tmp/$($slot.id)-web.stderr.log"
    }
}
Write-Output "Sandbox pool initialized: $registryPath"
