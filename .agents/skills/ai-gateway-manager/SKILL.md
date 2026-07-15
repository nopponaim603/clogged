---
name: ai-gateway-manager
description: Manage and setup local AI Gateway services (OmniRoute and 9Router) via Docker or Node.js / npm, helping debug and configure API routes, fallback configurations, and local coding agent proxies.
---

# AI Gateway Manager Skill

This skill helps configure, run, debug, and manage local AI Gateways in this workspace. There are two supported AI Gateways: **OmniRoute** (recommended) and **9Router**. Both run on port `20128` by default.

## Available Files & Components

- [setup-docker.cmd](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/setup-docker.cmd) - One-click CMD wrapper for Windows to execute the setup script bypassing Execution Policy.
- [setup-docker.ps1](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/setup-docker.ps1) - Main PowerShell setup wizard. Checks requirements, installs missing dependencies, and runs gateways.
- [omniroute/](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/omniroute/) - Contains OmniRoute configuration and docker-compose files.
- [9router/](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/9router/) - Contains 9Router configuration and docker-compose files.

---

## 🛠️ Step-by-Step Setup Guides

### 1. Automated Setup (Recommended)
To start the interactive wizard, run the CMD script:
```powershell
.\ai-gateway\setup-docker.cmd
```
The wizard will:
1. Allow you to choose between **Docker** and **Node.js/npm** deployment.
2. Verify and install requirements (Docker Desktop or Node.js LTS) using `winget` if missing.
3. Automatically attempt to launch Docker Desktop if it is installed but not running.
4. Let you select and spin up the gateway (OmniRoute or 9Router).

---

### 2. Manual Setup via Docker (OmniRoute or 9Router)

If you prefer to run manual commands:
1. Navigate to the desired gateway folder:
   - For OmniRoute: `cd ai-gateway/omniroute`
   - For 9Router: `cd ai-gateway/9router`
2. Spin up the container in detached mode:
   ```bash
   docker compose up -d
   ```
3. Stop the container:
   ```bash
   docker compose down
   ```

---

### 3. Manual Setup via Node.js / npm (OmniRoute Only)

If Docker is not desired, OmniRoute can run natively:
1. Install OmniRoute globally:
   ```bash
   npm install -g omniroute
   ```
2. Run the guided setup wizard to configure API keys:
   ```bash
   omniroute setup
   ```
3. Start the server:
   ```bash
   omniroute
   ```
4. Run diagnostics to verify API providers:
   ```bash
   omniroute doctor
   ```

---

## 🔍 Verification & Usage

Once started, verify the services using the following details:

### 1. Dashboards
- **OmniRoute Dashboard:** [http://localhost:20128/dashboard](http://localhost:20128/dashboard)
- **9Router Dashboard:** [http://localhost:20128](http://localhost:20128)

### 2. API Endpoint
To connect coding agents (e.g. Claude Code, Cursor, Cline):
- **Base URL:** `http://localhost:20128/v1` (OpenAI compatible)
- **API Key:** Use the API key generated or configured via the respective gateway dashboard.

---

## ⚠️ Troubleshooting & Best Practices

### Port Conflict (`20128`)
Both services listen on port `20128`. **Do not attempt to run both OmniRoute and 9Router simultaneously** unless you modify the port binding mapping in one of the `docker-compose.yml` files.

### Docker Daemon Not Running
If you receive standard pipe connection errors, ensure Docker Desktop is launched. The automated `setup-docker.ps1` script will try to automatically start it if located at `C:\Program Files\Docker\Docker\Docker Desktop.exe`.

### Git Security (Data Directory)
Local configurations, database files, and provider API keys are saved under `ai-gateway/omniroute/data/` or `ai-gateway/9router/data/`. These folders are gitignored in [.gitignore](file:///c:/Users/noppon/sources/01_DG/clogged/.gitignore) to protect credentials from being committed to Git. Never change this ignore rule.
