#!/usr/bin/env bash

# 9Router Local (Node.js) Setup & Run Script
# Optimized for Linux (openSUSE Leap)

# Color definitions
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Determine the absolute directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROUTER_DIR="$SCRIPT_DIR/9router"

show_header() {
    clear
    echo -e "${CYAN}========================================================${NC}"
    echo -e "${CYAN}         9Router Local Manager (Node.js / npm)          ${NC}"
    echo -e "${CYAN}========================================================${NC}"
    echo ""
}

check_dependencies() {
    # Check Node.js
    if ! command -v node &> /dev/null; then
        echo -e "${RED}[ERROR] Node.js is not installed on this system.${NC}"
        echo -e "Please install Node.js (v18+ recommended) and run this script again."
        exit 1
    fi

    # Check npm
    if ! command -v npm &> /dev/null; then
        echo -e "${RED}[ERROR] npm is not installed on this system.${NC}"
        exit 1
    fi
}

ensure_installed() {
    if [ ! -d "$ROUTER_DIR/node_modules" ] || [ ! -f "$ROUTER_DIR/package.json" ]; then
        echo -e "${YELLOW}[!] Local dependencies not found. Initializing and installing...${NC}"
        
        # Create directory if it doesn't exist
        mkdir -p "$ROUTER_DIR"
        cd "$ROUTER_DIR"
        
        if [ ! -f "package.json" ]; then
            npm init -y >/dev/null
        fi
        
        echo -e "${CYAN}Installing 9router locally (this may take a minute)...${NC}"
        npm install 9router --no-audit --no-fund
        
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}[SUCCESS] Local installation complete!${NC}"
            sleep 2
        else
            echo -e "${RED}[ERROR] Installation failed. Please check your internet connection.${NC}"
            exit 1
        fi
    fi
}

start_server() {
    local show_logs=$1
    show_header
    echo -e "${GREEN}[*] Starting 9Router server... (Press Ctrl+C to stop)${NC}"
    echo -e "${CYAN}Dashboard URL: http://localhost:20128${NC}"
    echo -e "${CYAN}API Endpoint:  http://localhost:20128/v1${NC}"
    echo ""
    cd "$ROUTER_DIR"
    
    if [ "$show_logs" = true ]; then
        npx 9router --log
    else
        npx 9router
    fi
}

reinstall_deps() {
    show_header
    echo -e "${YELLOW}[*] Reinstalling local 9router package...${NC}"
    cd "$ROUTER_DIR"
    rm -rf node_modules package-lock.json
    npm install 9router --no-audit --no-fund
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}[SUCCESS] Reinstalled successfully!${NC}"
    else
        echo -e "${RED}[ERROR] Reinstallation failed.${NC}"
    fi
    sleep 2
}

# Main script flow
check_dependencies
ensure_installed

while true; do
    show_header
    echo -e "Please select an option:"
    echo -e "  ${GREEN}[1] Start 9Router Server (Default mode)${NC}"
    echo -e "  [2] Start 9Router Server (With Logs enabled)${NC}"
    echo -e "  [3] Reinstall local dependencies${NC}"
    echo -e "  [4] Exit"
    echo -e "--------------------------------------------------------"
    echo ""
    read -p "Select option [1-4] (Default is 1): " choice
    
    # Default to 1 if enter is pressed
    if [ -z "$choice" ]; then
        choice="1"
    fi
    
    case "$choice" in
        1)
            start_server false
            ;;
        2)
            start_server true
            ;;
        3)
            reinstall_deps
            ;;
        4)
            echo "Exiting..."
            exit 0
            ;;
        *)
            echo -e "${RED}Invalid option. Please try again.${NC}"
            sleep 1.5
            ;;
    esac
done
