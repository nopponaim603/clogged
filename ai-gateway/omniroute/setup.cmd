@echo off
title OmniRoute Setup & Startup Helper

echo Checking Docker status...
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Docker is not running or not installed.
    echo Please make sure Docker Desktop is running and try again.
    echo.
    pause
    exit /b 1
)

:: Navigate to the directory of this script
cd /d "%~dp0"

echo.
echo [1/3] Launching OmniRoute via Docker Compose...
docker compose up -d

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Failed to launch Docker container.
    pause
    exit /b 1
)

echo.
echo [2/3] Waiting for service to initialize...
timeout /t 3 /nobreak >nul

echo.
echo [3/3] Opening OmniRoute Dashboard in browser...
start http://localhost:20128/dashboard

echo.
echo ========================================================
echo [SUCCESS] OmniRoute is now running in the background!
echo.
echo - Dashboard: http://localhost:20128/dashboard
echo - API Endpoint: http://localhost:20128/v1 (OpenAI compatible)
echo.
echo To stop OmniRoute in the future, run:
echo   docker compose down (inside the ai-gateway/omniroute/ directory)
echo ========================================================
echo.
pause
