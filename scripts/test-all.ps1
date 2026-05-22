# Run backend (.NET) and frontend (Angular/Vitest) test suites in sequence.
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "==> Running .NET tests" -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot 'src/api/SkyRoute')
try { dotnet test --nologo } finally { Pop-Location }

Write-Host "==> Running Angular tests" -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot 'src/web/sky-route')
try { npm test --silent } finally { Pop-Location }

Write-Host "All test suites passed." -ForegroundColor Green
