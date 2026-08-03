#!/usr/bin/env bash
set -euo pipefail

skip_install=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-install)
      skip_install=true
      shift
      ;;
    -h|--help)
      echo "Usage: ./setup-dev.sh [--skip-install]"
      echo "  --skip-install  Skip npm install if node_modules already exists"
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      echo "Usage: ./setup-dev.sh [--skip-install]" >&2
      exit 1
      ;;
  esac
done

project_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
cd "$project_dir"

ensure_nodejs() {
  if ! command -v node >/dev/null 2>&1; then
    echo "Node.js not found. Please install Node.js first." >&2
    echo "Example on Debian/Ubuntu: sudo apt update && sudo apt install -y nodejs npm" >&2
    exit 1
  fi

  if ! command -v npm >/dev/null 2>&1; then
    echo "npm not found. Please install npm first." >&2
    echo "Example on Debian/Ubuntu: sudo apt update && sudo apt install -y npm" >&2
    exit 1
  fi

  node -v
  npm -v
}

ensure_nodejs

if [[ "$skip_install" != true ]]; then
  if [[ ! -d "$project_dir/node_modules" ]]; then
    echo "Installing dependencies..."
    npm install
  else
    echo "Dependencies already installed."
  fi
fi

echo "Starting development server..."
npm run dev
