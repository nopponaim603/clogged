#!/usr/bin/env bash

# AI Gateway Local (Node.js) Setup & Run Script
# Optimized for Linux (openSUSE Leap)

# Color definitions
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Determine the absolute directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIROUTE_DIR="$SCRIPT_DIR/omniroute"

show_header() {
    clear
    echo -e "${CYAN}========================================================${NC}"
    echo -e "${CYAN}        OmniRoute Local Manager (Node.js / npm)        ${NC}"
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
    if [ ! -d "$OMNIROUTE_DIR/node_modules" ] || [ ! -f "$OMNIROUTE_DIR/package.json" ]; then
        echo -e "${YELLOW}[!] Local dependencies not found. Initializing and installing...${NC}"
        
        # Create directory if it doesn't exist
        mkdir -p "$OMNIROUTE_DIR"
        cd "$OMNIROUTE_DIR"
        
        if [ ! -f "package.json" ]; then
            npm init -y >/dev/null
        fi
        
        echo -e "${CYAN}Installing omniroute locally (this may take a minute)...${NC}"
        npm install omniroute --no-audit --no-fund
        
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
    show_header
    echo -e "${GREEN}[*] Starting OmniRoute server... (Press Ctrl+C to stop)${NC}"
    echo -e "${CYAN}Dashboard URL: http://localhost:20128/dashboard${NC}"
    echo -e "${CYAN}API Endpoint:  http://localhost:20128/v1${NC}"
    echo ""
    cd "$OMNIROUTE_DIR"
    npx omniroute
}

run_setup() {
    show_header
    echo -e "${CYAN}[*] Running OmniRoute Configuration Wizard...${NC}"
    echo ""
    cd "$OMNIROUTE_DIR"
    npx omniroute setup
    echo ""
    read -p "Press Enter to return to menu..."
}

run_doctor() {
    show_header
    echo -e "${CYAN}[*] Running OmniRoute System Doctor...${NC}"
    echo ""
    cd "$OMNIROUTE_DIR"
    npx omniroute doctor
    echo ""
    read -p "Press Enter to return to menu..."
}

show_status() {
    show_header
    echo -e "${CYAN}[*] Checking OmniRoute status...${NC}"
    echo ""
    cd "$OMNIROUTE_DIR"
    npx omniroute status
    echo ""
    read -p "Press Enter to return to menu..."
}

reinstall_deps() {
    show_header
    echo -e "${YELLOW}[*] Reinstalling local omniroute package...${NC}"
    cd "$OMNIROUTE_DIR"
    rm -rf node_modules package-lock.json
    npm install omniroute --no-audit --no-fund
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
    echo -e "  ${GREEN}[1] Start OmniRoute Server${NC}"
    echo -e "  [2] Run Configuration Wizard (Setup keys/providers)"
    echo -e "  [3] Run System Doctor (Verify connectivity)"
    echo -e "  [4] Check Server Status"
    echo -e "  ${YELLOW}[5] Reinstall local dependencies${NC}"
    echo -e "  [6] Exit"
    echo -e "--------------------------------------------------------"
    echo ""
    read -p "Select option [1-6] (Default is 1): " choice
    
    # Default to 1 if enter is pressed
    if [ -z "$choice" ]; then
        choice="1"
    fi
    
    case "$choice" in
        1)
            start_server
            ;;
        2)
            run_setup
            ;;
        3)
            run_doctor
            ;;
        4)
            show_status
            ;;
        5)
            reinstall_deps
            ;;
        6)
            echo "Exiting..."
            exit 0
            ;;
        *)
            echo -e "${RED}Invalid option. Please try again.${NC}"
            sleep 1.5
            ;;
    esac
done
