param(
    [string]$ApiOrigin = 'https://localhost:5001',
    [int]$PostgresPort = 5432,
    [int]$ApiPort = 5001,
    [int]$WebPort = 3000
)

$ErrorActionPreference = 'Stop'

function Get-PortOwners([int]$Port) {
    @(Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique |
        ForEach-Object {
            $process = Get-Process -Id $_ -ErrorAction SilentlyContinue
            if ($process) { "$($process.ProcessName) (PID $_)" } else { "PID $_" }
        })
}

$response = $null
try {
    $response = Invoke-WebRequest -Uri "$ApiOrigin/health/ready" -SkipCertificateCheck -TimeoutSec 5
} catch {
    $response = $_.Exception.Response
}

if ($response -and $response.StatusCode -eq 200) {
    Write-Output "Cove session is ready: $ApiOrigin/health/ready"
    exit 0
}

Write-Output "Cove session is not ready: API $ApiOrigin"
if ($response -and $response.StatusCode) { Write-Output "API readiness status: $($response.StatusCode)" }
else { Write-Output 'API readiness status: unreachable' }
foreach ($port in @($PostgresPort, $ApiPort, $WebPort)) {
    $owners = @(Get-PortOwners $port)
    if ($owners.Count -eq 0) { Write-Output "Port ${port}: available" }
    else { Write-Output "Port ${port}: $($owners -join ', ')" }
}
Write-Output "Session ports: PostgreSQL=$PostgresPort API=$ApiPort Web=$WebPort"
Write-Output 'Remediation: use a distinct port/state set for this session; do not stop another active session.'
exit 1
