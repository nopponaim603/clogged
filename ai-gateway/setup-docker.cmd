@echo off
title AI Gateway Docker Setup Helper
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup-docker.ps1"
