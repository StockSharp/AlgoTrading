#!/bin/bash

###############################################################################
# ARCHON TRIUMPH - Development Stop Script
# Safely stops all development processes
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Directories
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
PID_FILE="$PROJECT_DIR/.dev-pids"

# Functions
log() {
	echo -e "${GREEN}[$(date +'%H:%M:%S')]${NC} $1"
}

log_error() {
	echo -e "${RED}[$(date +'%H:%M:%S')] ERROR:${NC} $1"
}

log_warn() {
	echo -e "${YELLOW}[$(date +'%H:%M:%S')] WARN:${NC} $1"
}

log_success() {
	echo -e "${CYAN}[$(date +'%H:%M:%S')] ✓${NC} $1"
}

# Banner
echo -e "${CYAN}"
echo "════════════════════════════════════════════════════════════"
echo "           ARCHON TRIUMPH - Stopping Dev Mode"
echo "════════════════════════════════════════════════════════════"
echo -e "${NC}"

# Check if PID file exists
if [ ! -f "$PID_FILE" ]; then
	log_warn "No PID file found. Searching for running processes..."

	# Search for Electron processes
	ELECTRON_PIDS=$(pgrep -f "electron.*archon-triumph" || true)

	if [ -z "$ELECTRON_PIDS" ]; then
		log "No running ARCHON TRIUMPH processes found"
		exit 0
	fi

	log "Found Electron processes: $ELECTRON_PIDS"

	for pid in $ELECTRON_PIDS; do
		log "Stopping process $pid..."
		kill "$pid" 2>/dev/null || true
		sleep 1

		# Force kill if still running
		if kill -0 "$pid" 2>/dev/null; then
			log_warn "Force stopping process $pid..."
			kill -9 "$pid" 2>/dev/null || true
		fi
	done

	log_success "All processes stopped"
	exit 0
fi

# Stop processes from PID file
log "Stopping processes from PID file..."

while IFS= read -r pid; do
	if kill -0 "$pid" 2>/dev/null; then
		log "Stopping process $pid..."
		kill "$pid" 2>/dev/null || true
		sleep 1

		# Check if still running
		if kill -0 "$pid" 2>/dev/null; then
			log_warn "Process $pid did not stop gracefully. Force stopping..."
			kill -9 "$pid" 2>/dev/null || true
		fi

		log_success "Process $pid stopped"
	else
		log_warn "Process $pid not running"
	fi
done < "$PID_FILE"

# Remove PID file
rm "$PID_FILE"
log_success "PID file removed"

# Also check for any Python backend processes
PYTHON_PIDS=$(pgrep -f "python.*main.py" || true)
if [ -n "$PYTHON_PIDS" ]; then
	log "Found Python backend processes: $PYTHON_PIDS"

	for pid in $PYTHON_PIDS; do
		log "Stopping Python process $pid..."
		kill "$pid" 2>/dev/null || true
		sleep 1

		if kill -0 "$pid" 2>/dev/null; then
			kill -9 "$pid" 2>/dev/null || true
		fi
	done

	log_success "Python processes stopped"
fi

echo ""
log_success "All ARCHON TRIUMPH processes stopped successfully"
echo ""
