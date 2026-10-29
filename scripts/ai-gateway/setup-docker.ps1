# AI Gateway Setup & Installer Script (PowerShell)
# This file is written using 100% ASCII characters to prevent encoding mismatches in Windows PowerShell.

# Helper to clear screen and print header
function Show-Header {
    Clear-Host
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host "            AI Gateway Setup & Installer                " -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host ""
}

# Get the script's directory and change location to it
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
cd $scriptDir

function Start-DockerFlow {
    Show-Header
    Write-Host "[1/3] Checking Docker installation..." -ForegroundColor Cyan
    $dockerCmd = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerCmd) {
        Write-Host "[!] Docker CLI not found on this system." -ForegroundColor Yellow
        Write-Host "We recommend installing Docker Desktop to run AI Gateways in containers."
        Write-Host ""
        $choice = Read-Host "Do you want to install Docker Desktop automatically via winget? (Y/N)"
        if ($choice -eq 'y' -or $choice -eq 'Y' -or $choice -eq '') {
            Write-Host ""
            Write-Host "Installing Docker Desktop via winget... This may request Administrator permissions." -ForegroundColor Cyan
            winget install --id Docker.DockerDesktop --source winget
            if ($LastExitCode -eq 0) {
                Write-Host ""
                Write-Host "[SUCCESS] Docker Desktop installation initiated!" -ForegroundColor Green
                Write-Host "Please complete the installer GUI if it popped up, restart your computer/session, and run this script again." -ForegroundColor Green
                Write-Host ""
                Read-Host "Press Enter to exit..."
                Exit 0
            } else {
                Write-Host ""
                Write-Host "[ERROR] winget installation failed or was cancelled." -ForegroundColor Red
            }
        }
        
        Write-Host "Opening Docker Desktop download page in your browser..." -ForegroundColor Cyan
        Start-Process "https://www.docker.com/products/docker-desktop/"
        Write-Host "Please install Docker Desktop manually, make sure it is running, and try again." -ForegroundColor Yellow
        Write-Host ""
        Read-Host "Press Enter to exit..."
        Exit 1
    }

    # 2. Check if Docker Daemon is running
    Write-Host "[2/3] Checking if Docker service is running..." -ForegroundColor Cyan
    $dockerRunning = $false
    docker info >$null 2>&1
    if ($LastExitCode -ne 0) {
        Write-Host "Docker is installed but NOT running." -ForegroundColor Yellow
        
        # Try to start Docker Desktop on Windows
        $dockerPath = "C:\Program Files\Docker\Docker\Docker Desktop.exe"
        if (Test-Path $dockerPath) {
            Write-Host "Attempting to start Docker Desktop..." -ForegroundColor Cyan
            Start-Process $dockerPath
            
            Write-Host "Waiting for Docker daemon to initialize..." -ForegroundColor Cyan
            for ($i = 1; $i -le 10; $i++) {
                Start-Sleep -Seconds 5
                Write-Host "Checking status (attempt $i/10)..."
                docker info >$null 2>&1
                if ($LastExitCode -eq 0) {
                    $dockerRunning = $true
                    break
                }
            }
        }
    } else {
        $dockerRunning = $true
    }

    if (-not $dockerRunning) {
        Write-Host ""
        Write-Host "[ERROR] Docker daemon is not running." -ForegroundColor Red
        Write-Host "Please start Docker Desktop manually and run this script again." -ForegroundColor Red
        Write-Host ""
        Read-Host "Press Enter to exit..."
        Exit 1
    }

    Write-Host "[OK] Docker is running." -ForegroundColor Green
    Write-Host ""

    # 3. Present Gateway Selection Menu
    Write-Host "--------------------------------------------------------"
    Write-Host "Please select the AI Gateway to run via Docker:" -ForegroundColor Cyan
    Write-Host "  [1] OmniRoute (Recommended: 250+ Providers, compression, auto-fallback)"
    Write-Host "  [2] 9Router (Alternative: lightweight, fallback, Rust Token Killer)"
    Write-Host "  [3] Go Back to Main Menu"
    Write-Host "  [4] Exit"
    Write-Host "--------------------------------------------------------"
    Write-Host ""

    $choice = Read-Host "Select option [1-4] (Default is 1)"
    if ($choice -eq "" -or $choice -eq "1") {
        Write-Host ""
        Write-Host "Launching OmniRoute setup..." -ForegroundColor Cyan
        if (Test-Path ".\omniroute\setup.ps1") {
            cd ".\omniroute"
            & ".\setup.ps1"
        } else {
            Write-Host "[ERROR] Script not found: .\omniroute\setup.ps1" -ForegroundColor Red
            Read-Host "Press Enter to exit..."
        }
    }
    elseif ($choice -eq "2") {
        Write-Host ""
        Write-Host "Launching 9Router setup..." -ForegroundColor Cyan
        if (Test-Path ".\9router\setup.ps1") {
            cd ".\9router"
            & ".\setup.ps1"
        } else {
            Write-Host "[ERROR] Script not found: .\9router\setup.ps1" -ForegroundColor Red
            Read-Host "Press Enter to exit..."
        }
    }
    elseif ($choice -eq "3") {
        Start-MainMenu
    }
    else {
        Write-Host "Exiting..."
        Exit 0
    }
}

function Start-NodeFlow {
    Show-Header
    Write-Host "[1/3] Checking Node.js and npm installation..." -ForegroundColor Cyan
    $nodeCmd = Get-Command node -ErrorAction SilentlyContinue
    $npmCmd = Get-Command npm -ErrorAction SilentlyContinue

    if (-not $nodeCmd -or -not $npmCmd) {
        Write-Host "[!] Node.js or npm not found on this system." -ForegroundColor Yellow
        Write-Host "OmniRoute requires Node.js (LTS version recommended) to run directly."
        Write-Host ""
        $choice = Read-Host "Do you want to install Node.js LTS automatically via winget? (Y/N)"
        if ($choice -eq 'y' -or $choice -eq 'Y' -or $choice -eq '') {
            Write-Host ""
            Write-Host "Installing Node.js LTS via winget..." -ForegroundColor Cyan
            winget install --id OpenJS.NodeJS.LTS --source winget
            if ($LastExitCode -eq 0) {
                Write-Host ""
                Write-Host "[SUCCESS] Node.js LTS installation initiated!" -ForegroundColor Green
                Write-Host "Please complete the installer GUI if it popped up, close and reopen your terminal/session to reload PATH, and run this script again." -ForegroundColor Green
                Write-Host ""
                Read-Host "Press Enter to exit..."
                Exit 0
            } else {
                Write-Host ""
                Write-Host "[ERROR] winget installation failed or was cancelled." -ForegroundColor Red
            }
        }

        Write-Host "Opening Node.js download page in your browser..." -ForegroundColor Cyan
        Start-Process "https://nodejs.org/"
        Write-Host "Please install Node.js manually, reload your session, and try again." -ForegroundColor Yellow
        Write-Host ""
        Read-Host "Press Enter to exit..."
        Exit 1
    }

    $nodeVersion = node -v
    Write-Host "[OK] Node.js is installed ($nodeVersion)" -ForegroundColor Green
    Write-Host ""

    # 2. Check if OmniRoute is installed globally
    Write-Host "[2/3] Checking OmniRoute installation..." -ForegroundColor Cyan
    $omnirouteCmd = Get-Command omniroute -ErrorAction SilentlyContinue
    if (-not $omnirouteCmd) {
        Write-Host "OmniRoute is not installed globally on this system." -ForegroundColor Yellow
        $choice = Read-Host "Do you want to install OmniRoute globally via npm? (Y/N)"
        if ($choice -eq 'y' -or $choice -eq 'Y' -or $choice -eq '') {
            Write-Host ""
            Write-Host "Installing OmniRoute globally (npm install -g omniroute)..." -ForegroundColor Cyan
            npm install -g omniroute
            if ($LastExitCode -ne 0) {
                Write-Host "[ERROR] Failed to install OmniRoute." -ForegroundColor Red
                Read-Host "Press Enter to exit..."
                Exit 1
            }
            Write-Host "[OK] OmniRoute installed successfully!" -ForegroundColor Green
        } else {
            Write-Host "Cancelled OmniRoute installation." -ForegroundColor Yellow
            Read-Host "Press Enter to exit..."
            Exit 0
        }
    } else {
        Write-Host "[OK] OmniRoute is installed." -ForegroundColor Green
    }
    Write-Host ""

    # 3. Present OmniRoute Commands Menu
    Show-Header
    Write-Host "--------------------------------------------------------"
    Write-Host "OmniRoute Node.js / npm Menu:" -ForegroundColor Cyan
    Write-Host "  [1] Start OmniRoute Server"
    Write-Host "  [2] Run Configuration Wizard"
    Write-Host "  [3] Run System Doctor (Check connectivity)"
    Write-Host "  [4] Go Back to Main Menu"
    Write-Host "  [5] Exit"
    Write-Host "--------------------------------------------------------"
    Write-Host ""

    $choice = Read-Host "Select option [1-5] (Default is 1)"
    if ($choice -eq "" -or $choice -eq "1") {
        Write-Host ""
        Write-Host "Starting OmniRoute server (Press Ctrl+C to stop)..." -ForegroundColor Green
        omniroute
    }
    elseif ($choice -eq "2") {
        Write-Host ""
        Write-Host "Running configuration wizard..." -ForegroundColor Cyan
        omniroute setup
    }
    elseif ($choice -eq "3") {
        Write-Host ""
        Write-Host "Running system doctor..." -ForegroundColor Cyan
        omniroute doctor
        Write-Host ""
        Read-Host "Press Enter to continue..."
        Start-NodeFlow
    }
    elseif ($choice -eq "4") {
        Start-MainMenu
    }
    else {
        Write-Host "Exiting..."
        Exit 0
    }
}

function Start-MainMenu {
    Show-Header
    Write-Host "Please select how you want to run the AI Gateway:" -ForegroundColor Cyan
    Write-Host "  [1] Run via Docker Container (Recommended)"
    Write-Host "  [2] Run via Node.js / npm (Direct native installation of OmniRoute)"
    Write-Host "  [3] Exit"
    Write-Host "--------------------------------------------------------"
    Write-Host ""

    $choice = Read-Host "Select option [1-3] (Default is 1)"
    if ($choice -eq "" -or $choice -eq "1") {
        Start-DockerFlow
    }
    elseif ($choice -eq "2") {
        Start-NodeFlow
    }
    else {
        Write-Host "Exiting..."
        Exit 0
    }
}

# Run Main Menu
Start-MainMenu
