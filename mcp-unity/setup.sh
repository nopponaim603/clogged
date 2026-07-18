#!/usr/bin/env bash

# MCP Unity Local Setup Helper for Linux
# This script clones the third-party Unity MCP bridge, installs dependencies, and builds the server.

# Color definitions
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

set -e

# Get script directory and change location to it
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo -e "${CYAN}========================================================${NC}"
echo -e "${CYAN}            MCP Unity Local Setup Helper                ${NC}"
echo -e "${CYAN}========================================================${NC}"
echo ""

# 1. Check prerequisites
echo -e "${CYAN}[1/4] Checking prerequisites...${NC}"
PREREQS=("git" "node" "npm")
for cmd in "${PREREQS[@]}"; do
    if command -v "$cmd" &>/dev/null; then
        echo -e "  - $cmd: ${GREEN}Found${NC}"
    else
        echo -e "${RED}[ERROR] $cmd is not installed or not in PATH.${NC}"
        echo -e "Please install it and try again."
        echo ""
        read -p "Press Enter to exit..."
        exit 1
    fi
done
echo ""

# 2. Clone external repository
echo -e "${CYAN}[2/4] Cloning/Updating CoderGamester/mcp-unity...${NC}"
CLONED_DIR="$SCRIPT_DIR/cloned"
REPO_URL="https://github.com/CoderGamester/mcp-unity.git"

if [ ! -d "$CLONED_DIR" ]; then
    echo -e "  Cloning repository into $CLONED_DIR..."
    git clone "$REPO_URL" cloned
else
    echo -e "  Repository already cloned. Pulling latest updates..."
    cd "$CLONED_DIR"
    if git pull; then
        echo -e "  ${GREEN}Successfully pulled latest updates.${NC}"
    else
        echo -e "  ${YELLOW}[Warning] Failed to git pull (perhaps offline or uncommitted changes). Proceeding...${NC}"
    fi
    cd "$SCRIPT_DIR"
fi
echo ""

# 3. Install dependencies and Build Server
echo -e "${CYAN}[3/4] Building MCP Unity Server...${NC}"
SERVER_DIR="$CLONED_DIR/Server~"

if [ -d "$SERVER_DIR" ]; then
    cd "$SERVER_DIR"
    echo -e "  Running npm install in Server~..."
    npm install
    
    echo -e "  Running npm run build in Server~..."
    npm run build
    
    cd "$SCRIPT_DIR"
else
    echo -e "${RED}[ERROR] Server~ directory not found inside the clone!${NC}"
    read -p "Press Enter to exit..."
    exit 1
fi
echo ""

# 4. Automatically update configuration files
echo -e "${CYAN}[4/4] Configuring AI Gateway / workspace settings...${NC}"

# Run Node script to parse and update json files safely
node -e '
const fs = require("fs");
const path = require("path");

// Update kilo.json at root
const kiloPath = path.join("..", "kilo.json");
if (fs.existsSync(kiloPath)) {
    console.log("  Updating kilo.json...");
    try {
        const kiloContent = JSON.parse(fs.readFileSync(kiloPath, "utf8"));
        if (kiloContent.mcp && kiloContent.mcp["mcp-unity"]) {
            kiloContent.mcp["mcp-unity"].command = ["node", "mcp-unity/cloned/Server~/build/index.js"];
            fs.writeFileSync(kiloPath, JSON.stringify(kiloContent, null, 2), "utf8");
            console.log("\x1b[32m    Successfully updated kilo.json to use local mcp-unity server.\x1b[0m");
        }
    } catch (e) {
        console.error("\x1b[31m    Failed to update kilo.json: " + e.message + "\x1b[0m");
    }
}

// Update opencode.json in Unity-Projects
const opencodePath = path.join("..", "Unity-Projects", "opencode.json");
if (fs.existsSync(opencodePath)) {
    console.log("  Updating opencode.json...");
    try {
        const opencodeContent = JSON.parse(fs.readFileSync(opencodePath, "utf8"));
        if (opencodeContent.mcp && opencodeContent.mcp["mcp-unity"]) {
            opencodeContent.mcp["mcp-unity"].command = ["node", "../mcp-unity/cloned/Server~/build/index.js"];
            fs.writeFileSync(opencodePath, JSON.stringify(opencodeContent, null, 2), "utf8");
            console.log("\x1b[32m    Successfully updated opencode.json to use local mcp-unity server.\x1b[0m");
        }
    } catch (e) {
        console.error("\x1b[31m    Failed to update opencode.json: " + e.message + "\x1b[0m");
    }
}
'

echo ""
echo -e "${GREEN}========================================================${NC}"
echo -e "${GREEN}[SUCCESS] mcp-unity local setup complete!${NC}"
echo -e "${GREEN}The MCP Server path is: mcp-unity/cloned/Server~/build/index.js${NC}"
echo -e "${GREEN}========================================================${NC}"
echo ""
read -p "Press Enter to exit..."
