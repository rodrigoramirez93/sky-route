# Bring up the full stack (api + web + aspire dashboard) via docker compose.
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    docker compose up --build
}
finally {
    Pop-Location
}
