Write-Host "Checking Docker status..." -ForegroundColor Cyan
docker info >$null 2>&1
if ($LastExitCode -ne 0) {
    Write-Host ""
    Write-Host "[ERROR] Docker is not running or not installed." -ForegroundColor Red
    Write-Host "Please make sure Docker Desktop is running and try again." -ForegroundColor Red
    Write-Host ""
    Read-Host "Press Enter to exit..."
    Exit 1
}

# Navigate to the directory of this script
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
cd $scriptDir

Write-Host ""
Write-Host "[1/3] Launching 9Router via Docker Compose..." -ForegroundColor Cyan
docker compose up -d

if ($LastExitCode -ne 0) {
    Write-Host ""
    Write-Host "[ERROR] Failed to launch Docker container." -ForegroundColor Red
    Read-Host "Press Enter to exit..."
    Exit 1
}

Write-Host ""
Write-Host "[2/3] Waiting for service to initialize..." -ForegroundColor Cyan
Start-Sleep -Seconds 3

Write-Host ""
Write-Host "[3/3] Opening 9Router Dashboard in browser..." -ForegroundColor Cyan
Start-Process "http://localhost:20128"

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "[SUCCESS] 9Router is now running in the background!" -ForegroundColor Green
Write-Host ""
Write-Host "- Dashboard: http://localhost:20128"
Write-Host "- API Endpoint: http://localhost:20128/v1 (OpenAI compatible)"
Write-Host ""
Write-Host "To stop 9Router in the future, run:"
Write-Host "  docker compose down (inside the docker/ directory)"
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""
Read-Host "Press Enter to exit..."
