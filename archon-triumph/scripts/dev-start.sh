#!/bin/bash

###############################################################################
# ARCHON TRIUMPH - Development Launcher
# Starts the application in development mode with detailed logging
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Directories
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
BACKEND_DIR="$PROJECT_DIR/backend"
LOGS_DIR="$PROJECT_DIR/logs"

# Log file
LOG_FILE="$LOGS_DIR/dev-$(date +%Y%m%d-%H%M%S).log"

# PID file
PID_FILE="$PROJECT_DIR/.dev-pids"

# Functions
log() {
	echo -e "${GREEN}[$(date +'%H:%M:%S')]${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
	echo -e "${RED}[$(date +'%H:%M:%S')] ERROR:${NC} $1" | tee -a "$LOG_FILE"
}

log_warn() {
	echo -e "${YELLOW}[$(date +'%H:%M:%S')] WARN:${NC} $1" | tee -a "$LOG_FILE"
}

log_info() {
	echo -e "${BLUE}[$(date +'%H:%M:%S')] INFO:${NC} $1" | tee -a "$LOG_FILE"
}

log_success() {
	echo -e "${CYAN}[$(date +'%H:%M:%S')] ✓${NC} $1" | tee -a "$LOG_FILE"
}

# Banner
print_banner() {
	echo -e "${CYAN}"
	echo "════════════════════════════════════════════════════════════"
	echo "           ARCHON TRIUMPH - Development Mode"
	echo "════════════════════════════════════════════════════════════"
	echo -e "${NC}"
}

# Check if already running
check_running() {
	if [ -f "$PID_FILE" ]; then
		log_warn "Found existing PID file. Checking processes..."

		while IFS= read -r pid; do
			if kill -0 "$pid" 2>/dev/null; then
				log_error "Process $pid is still running!"
				log_error "Please run ./dev-stop.sh first"
				exit 1
			fi
		done < "$PID_FILE"

		# Clean up stale PID file
		rm "$PID_FILE"
	fi
}

# Check prerequisites
check_prerequisites() {
	log "Checking prerequisites..."

	# Check Node.js
	if ! command -v node &> /dev/null; then
		log_error "Node.js is not installed!"
		exit 1
	fi
	log_success "Node.js: $(node --version)"

	# Check npm
	if ! command -v npm &> /dev/null; then
		log_error "npm is not installed!"
		exit 1
	fi
	log_success "npm: $(npm --version)"

	# Check Python
	if command -v python3 &> /dev/null; then
		PYTHON_CMD="python3"
	elif command -v python &> /dev/null; then
		PYTHON_CMD="python"
	else
		log_error "Python is not installed!"
		exit 1
	fi
	log_success "Python: $($PYTHON_CMD --version)"

	# Check pip
	if ! $PYTHON_CMD -m pip --version &> /dev/null; then
		log_error "pip is not installed!"
		exit 1
	fi
	log_success "pip: $($PYTHON_CMD -m pip --version)"
}

# Setup directories
setup_directories() {
	log "Setting up directories..."

	mkdir -p "$LOGS_DIR"
	log_success "Logs directory: $LOGS_DIR"
}

# Install Node dependencies
install_node_deps() {
	log "Checking Node.js dependencies..."

	cd "$PROJECT_DIR"

	if [ ! -d "node_modules" ]; then
		log "Installing Node.js dependencies..."
		npm install
		log_success "Node.js dependencies installed"
	else
		log_success "Node.js dependencies already installed"
	fi
}

# Install Python dependencies
install_python_deps() {
	log "Checking Python dependencies..."

	cd "$BACKEND_DIR"

	if [ -f "requirements.txt" ]; then
		log "Installing Python dependencies..."
		$PYTHON_CMD -m pip install -r requirements.txt --quiet
		log_success "Python dependencies installed"
	else
		log_warn "requirements.txt not found"
	fi
}

# Cleanup on exit
cleanup() {
	log ""
	log "Cleaning up..."

	if [ -f "$PID_FILE" ]; then
		while IFS= read -r pid; do
			if kill -0 "$pid" 2>/dev/null; then
				log "Stopping process $pid..."
				kill "$pid" 2>/dev/null || true
			fi
		done < "$PID_FILE"

		rm "$PID_FILE"
	fi

	log_success "Cleanup complete"
	exit 0
}

# Trap signals
trap cleanup SIGINT SIGTERM EXIT

# Start application
start_app() {
	log "Starting ARCHON TRIUMPH in development mode..."

	cd "$PROJECT_DIR"

	# Set environment
	export NODE_ENV=development
	export ARCHON_HOST=127.0.0.1
	export ARCHON_PORT=8000

	log_info "Environment: $NODE_ENV"
	log_info "Backend: $ARCHON_HOST:$ARCHON_PORT"

	# Start Electron
	log "Launching Electron application..."
	npm run dev &
	ELECTRON_PID=$!
	echo "$ELECTRON_PID" > "$PID_FILE"

	log_success "Electron started (PID: $ELECTRON_PID)"
	log ""
	log_info "Application is starting..."
	log_info "Logs are being written to: $LOG_FILE"
	log ""
	log "Press Ctrl+C to stop the application"
	log ""

	# Wait for Electron process
	wait $ELECTRON_PID
}

# Main
main() {
	print_banner

	check_running
	check_prerequisites
	setup_directories
	install_node_deps
	install_python_deps

	log ""
	start_app
}

# Run
main
