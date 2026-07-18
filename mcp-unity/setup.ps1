# PowerShell script to setup mcp-unity locally
# This script clones the third-party Unity MCP bridge, installs dependencies, and builds the server.

$ErrorActionPreference = "Stop"

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
cd $scriptDir

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "            MCP Unity Local Setup Helper" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Check prerequisites
Write-Host "[1/4] Checking prerequisites..." -ForegroundColor Cyan
$prereqs = @{
    "git" = "git --version"
    "node" = "node --version"
    "npm" = "npm --version"
}

foreach ($cmd in $prereqs.Keys) {
    try {
        Invoke-Expression $prereqs[$cmd] > $null 2>&1
        Write-Host "  - ${cmd}: Found" -ForegroundColor Green
    } catch {
        Write-Host ""
        Write-Host "[ERROR] $cmd is not installed or not in PATH." -ForegroundColor Red
        Write-Host "Please install it and try again." -ForegroundColor Red
        Write-Host ""
        Read-Host "Press Enter to exit..."
        Exit 1
    }
}
Write-Host ""

# 2. Clone external repository
Write-Host "[2/4] Cloning/Updating CoderGamester/mcp-unity..." -ForegroundColor Cyan
$clonedDir = Join-Path $scriptDir "cloned"
$repoUrl = "https://github.com/CoderGamester/mcp-unity.git"

if (-not (Test-Path $clonedDir)) {
    Write-Host "  Cloning repository into $clonedDir..." -ForegroundColor Yellow
    git clone $repoUrl cloned
} else {
    Write-Host "  Repository already cloned. Pulling latest updates..." -ForegroundColor Yellow
    cd cloned
    try {
        git pull
    } catch {
        Write-Host "  [Warning] Failed to git pull (perhaps offline or uncommitted changes). Proceeding..." -ForegroundColor Yellow
    }
    cd $scriptDir
}
Write-Host ""

# 3. Install dependencies and Build Server
Write-Host "[3/4] Building MCP Unity Server..." -ForegroundColor Cyan
$serverDir = Join-Path (Join-Path $scriptDir "cloned") "Server~"

if (Test-Path $serverDir) {
    cd $serverDir
    Write-Host "  Running npm install in Server~..." -ForegroundColor Yellow
    npm install
    
    Write-Host "  Running npm run build in Server~..." -ForegroundColor Yellow
    npm run build
    
    cd $scriptDir
} else {
    Write-Host "[ERROR] Server~ directory not found inside the clone!" -ForegroundColor Red
    Read-Host "Press Enter to exit..."
    Exit 1
}
Write-Host ""

# 4. Automatically update configuration files
Write-Host "[4/4] Configuring AI Gateway / workspace settings..." -ForegroundColor Cyan

# Update kilo.json at root
$kiloPath = Join-Path $scriptDir "..\kilo.json"
if (Test-Path $kiloPath) {
    Write-Host "  Updating $kiloPath..." -ForegroundColor Yellow
    $kiloContent = Get-Content $kiloPath -Raw | ConvertFrom-Json
    if ($kiloContent.mcp -and $kiloContent.mcp."mcp-unity") {
        $kiloContent.mcp."mcp-unity".command = @("node", '${workspaceFolder}/mcp-unity/cloned/Server~/build/index.js')
        $kiloContent | ConvertTo-Json -Depth 10 | Set-Content $kiloPath
        Write-Host "    Successfully updated kilo.json to use local mcp-unity server." -ForegroundColor Green
    }
}

# Update opencode.json in Unity-Projects
$opencodePath = Join-Path $scriptDir "..\Unity-Projects\opencode.json"
if (Test-Path $opencodePath) {
    Write-Host "  Updating $opencodePath..." -ForegroundColor Yellow
    $opencodeContent = Get-Content $opencodePath -Raw | ConvertFrom-Json
    if ($opencodeContent.mcp -and $opencodeContent.mcp."mcp-unity") {
        $opencodeContent.mcp."mcp-unity".command = @("node", "../mcp-unity/cloned/Server~/build/index.js")
        $opencodeContent | ConvertTo-Json -Depth 10 | Set-Content $opencodePath
        Write-Host "    Successfully updated opencode.json to use local mcp-unity server." -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "[SUCCESS] mcp-unity local setup complete!" -ForegroundColor Green
Write-Host "The MCP Server path is: mcp-unity/cloned/Server~/build/index.js" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""
Read-Host "Press Enter to exit..."
