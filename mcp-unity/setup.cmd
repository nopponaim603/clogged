@echo off
title MCP Unity Local Setup Helper
cd /d "%~dp0"

echo Running local setup via PowerShell...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup.ps1"
