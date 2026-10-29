param(
    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectDir

function Ensure-NodeJs {
    if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
        Write-Host "Node.js not found. Installing via winget..."
        winget install --id OpenJS.NodeJS.LTS -e --source winget

        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
        $env:Path = $env:Path + ";" + $env:Path
    }

    if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
        Write-Host "npm not found. Installing via winget..."
        winget install --id OpenJS.NodeJS.LTS -e --source winget
    }

    node -v
    npm -v
}

Ensure-NodeJs

if (-not $SkipInstall) {
    if (-not (Test-Path "$projectDir/node_modules")) {
        Write-Host "Installing dependencies..."
        npm install
    }
    else {
        Write-Host "Dependencies already installed."
    }
}

Write-Host "Starting development server..."
npm run dev
