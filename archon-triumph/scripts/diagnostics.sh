#!/bin/bash

###############################################################################
# ARCHON TRIUMPH - Diagnostic Tool
# Comprehensive system diagnostics and health check
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
NC='\033[0m' # No Color

# Directories
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
BACKEND_DIR="$PROJECT_DIR/backend"
LOGS_DIR="$PROJECT_DIR/logs"
PID_FILE="$PROJECT_DIR/.dev-pids"

# Diagnostic report file
REPORT_FILE="$PROJECT_DIR/diagnostic-report-$(date +%Y%m%d-%H%M%S).txt"

# Functions
log() {
	echo -e "${GREEN}[$(date +'%H:%M:%S')]${NC} $1"
}

log_error() {
	echo -e "${RED}[$(date +'%H:%M:%S')] ✗${NC} $1"
}

log_warn() {
	echo -e "${YELLOW}[$(date +'%H:%M:%S')] ⚠${NC} $1"
}

log_success() {
	echo -e "${CYAN}[$(date +'%H:%M:%S')] ✓${NC} $1"
}

log_section() {
	echo -e "${MAGENTA}[$(date +'%H:%M:%S')] ▶${NC} $1"
}

log_info() {
	echo -e "${BLUE}[$(date +'%H:%M:%S')] ℹ${NC} $1"
}

# Write to report
write_report() {
	echo "$1" >> "$REPORT_FILE"
}

# Banner
print_banner() {
	echo -e "${CYAN}"
	echo "════════════════════════════════════════════════════════════"
	echo "           ARCHON TRIUMPH - System Diagnostics"
	echo "════════════════════════════════════════════════════════════"
	echo -e "${NC}"
}

# Initialize report
init_report() {
	cat > "$REPORT_FILE" << EOF
ARCHON TRIUMPH - Diagnostic Report
════════════════════════════════════════════════════════════
Generated: $(date)
Platform: $(uname -s) $(uname -m)
════════════════════════════════════════════════════════════

EOF
}

# Check system information
check_system_info() {
	log_section "System Information"
	write_report "SYSTEM INFORMATION"
	write_report "$(printf '%.s-' {1..60})"

	# Operating System
	if [ -f /etc/os-release ]; then
		. /etc/os-release
		log_info "OS: $NAME $VERSION"
		write_report "OS: $NAME $VERSION"
	else
		log_info "OS: $(uname -s)"
		write_report "OS: $(uname -s)"
	fi

	# Kernel
	log_info "Kernel: $(uname -r)"
	write_report "Kernel: $(uname -r)"

	# Architecture
	log_info "Architecture: $(uname -m)"
	write_report "Architecture: $(uname -m)"

	# CPU
	if [ -f /proc/cpuinfo ]; then
		CPU_MODEL=$(grep "model name" /proc/cpuinfo | head -1 | cut -d: -f2 | xargs)
		CPU_CORES=$(grep "processor" /proc/cpuinfo | wc -l)
		log_info "CPU: $CPU_MODEL ($CPU_CORES cores)"
		write_report "CPU: $CPU_MODEL ($CPU_CORES cores)"
	fi

	# Memory
	if [ -f /proc/meminfo ]; then
		TOTAL_MEM=$(grep MemTotal /proc/meminfo | awk '{print $2}')
		TOTAL_MEM_GB=$(echo "scale=2; $TOTAL_MEM/1024/1024" | bc)
		log_info "Memory: ${TOTAL_MEM_GB}GB"
		write_report "Memory: ${TOTAL_MEM_GB}GB"
	fi

	write_report ""
	echo ""
}

# Check dependencies
check_dependencies() {
	log_section "Checking Dependencies"
	write_report "DEPENDENCIES"
	write_report "$(printf '%.s-' {1..60})"

	local all_ok=true

	# Node.js
	if command -v node &> /dev/null; then
		NODE_VER=$(node --version)
		log_success "Node.js: $NODE_VER"
		write_report "✓ Node.js: $NODE_VER"
	else
		log_error "Node.js: NOT FOUND"
		write_report "✗ Node.js: NOT FOUND"
		all_ok=false
	fi

	# npm
	if command -v npm &> /dev/null; then
		NPM_VER=$(npm --version)
		log_success "npm: $NPM_VER"
		write_report "✓ npm: $NPM_VER"
	else
		log_error "npm: NOT FOUND"
		write_report "✗ npm: NOT FOUND"
		all_ok=false
	fi

	# Python
	if command -v python3 &> /dev/null; then
		PYTHON_VER=$(python3 --version)
		log_success "Python: $PYTHON_VER"
		write_report "✓ Python: $PYTHON_VER"
	elif command -v python &> /dev/null; then
		PYTHON_VER=$(python --version)
		log_success "Python: $PYTHON_VER"
		write_report "✓ Python: $PYTHON_VER"
	else
		log_error "Python: NOT FOUND"
		write_report "✗ Python: NOT FOUND"
		all_ok=false
	fi

	# pip
	if command -v pip3 &> /dev/null; then
		PIP_VER=$(pip3 --version)
		log_success "pip: $PIP_VER"
		write_report "✓ pip: $PIP_VER"
	elif command -v pip &> /dev/null; then
		PIP_VER=$(pip --version)
		log_success "pip: $PIP_VER"
		write_report "✓ pip: $PIP_VER"
	else
		log_error "pip: NOT FOUND"
		write_report "✗ pip: NOT FOUND"
		all_ok=false
	fi

	write_report ""
	echo ""

	if [ "$all_ok" = true ]; then
		log_success "All required dependencies are installed"
	else
		log_error "Some dependencies are missing!"
	fi
}

# Check project structure
check_project_structure() {
	log_section "Checking Project Structure"
	write_report "PROJECT STRUCTURE"
	write_report "$(printf '%.s-' {1..60})"

	local dirs=(
		"backend"
		"backend/config"
		"electron"
		"frontend"
		"frontend/css"
		"frontend/js"
		"scripts"
		"build"
		"logs"
	)

	local all_ok=true

	for dir in "${dirs[@]}"; do
		if [ -d "$PROJECT_DIR/$dir" ]; then
			log_success "$dir/"
			write_report "✓ $dir/"
		else
			log_error "$dir/ - MISSING"
			write_report "✗ $dir/ - MISSING"
			all_ok=false
		fi
	done

	write_report ""
	echo ""

	# Check key files
	log_info "Checking key files..."
	write_report "KEY FILES"
	write_report "$(printf '%.s-' {1..60})"

	local files=(
		"package.json"
		"backend/main.py"
		"backend/requirements.txt"
		"electron/main.js"
		"electron/backend.js"
		"electron/preload.js"
		"electron/ipc.js"
		"frontend/index.html"
	)

	for file in "${files[@]}"; do
		if [ -f "$PROJECT_DIR/$file" ]; then
			size=$(du -h "$PROJECT_DIR/$file" | cut -f1)
			log_success "$file ($size)"
			write_report "✓ $file ($size)"
		else
			log_error "$file - MISSING"
			write_report "✗ $file - MISSING"
			all_ok=false
		fi
	done

	write_report ""
	echo ""
}

# Check running processes
check_processes() {
	log_section "Checking Running Processes"
	write_report "RUNNING PROCESSES"
	write_report "$(printf '%.s-' {1..60})"

	# Check PID file
	if [ -f "$PID_FILE" ]; then
		log_info "PID file found: $PID_FILE"
		write_report "PID file found"

		while IFS= read -r pid; do
			if kill -0 "$pid" 2>/dev/null; then
				PROC_INFO=$(ps -p "$pid" -o pid,comm,etime,pcpu,pmem --no-headers)
				log_success "Process $pid is running"
				write_report "✓ Process $pid: $PROC_INFO"
			else
				log_warn "Process $pid is not running (stale PID)"
				write_report "⚠ Process $pid: NOT RUNNING (stale)"
			fi
		done < "$PID_FILE"
	else
		log_info "No PID file found"
		write_report "No PID file found"
	fi

	# Search for Electron processes
	ELECTRON_PIDS=$(pgrep -f "electron.*archon-triumph" || true)
	if [ -n "$ELECTRON_PIDS" ]; then
		log_info "Found Electron processes: $ELECTRON_PIDS"
		write_report "Electron processes: $ELECTRON_PIDS"
	else
		log_info "No Electron processes found"
		write_report "No Electron processes found"
	fi

	# Search for Python backend
	PYTHON_PIDS=$(pgrep -f "python.*main.py" || true)
	if [ -n "$PYTHON_PIDS" ]; then
		log_info "Found Python backend processes: $PYTHON_PIDS"
		write_report "Python backend processes: $PYTHON_PIDS"
	else
		log_info "No Python backend processes found"
		write_report "No Python backend processes found"
	fi

	write_report ""
	echo ""
}

# Check network ports
check_network() {
	log_section "Checking Network Ports"
	write_report "NETWORK PORTS"
	write_report "$(printf '%.s-' {1..60})"

	# Check if backend port is in use
	BACKEND_PORT=8000

	if command -v netstat &> /dev/null; then
		PORT_STATUS=$(netstat -tuln | grep ":$BACKEND_PORT " || true)
	elif command -v ss &> /dev/null; then
		PORT_STATUS=$(ss -tuln | grep ":$BACKEND_PORT " || true)
	else
		PORT_STATUS=""
	fi

	if [ -n "$PORT_STATUS" ]; then
		log_success "Backend port $BACKEND_PORT is in use"
		write_report "✓ Port $BACKEND_PORT: IN USE"
		write_report "$PORT_STATUS"
	else
		log_warn "Backend port $BACKEND_PORT is not in use"
		write_report "⚠ Port $BACKEND_PORT: NOT IN USE"
	fi

	# Try to connect to backend
	if command -v curl &> /dev/null; then
		log_info "Testing backend connection..."
		if curl -s "http://127.0.0.1:$BACKEND_PORT/health" > /dev/null 2>&1; then
			log_success "Backend health check: OK"
			write_report "✓ Backend health check: OK"
		else
			log_warn "Backend health check: FAILED"
			write_report "⚠ Backend health check: FAILED"
		fi
	fi

	write_report ""
	echo ""
}

# Check logs
check_logs() {
	log_section "Checking Logs"
	write_report "LOGS"
	write_report "$(printf '%.s-' {1..60})"

	if [ -d "$LOGS_DIR" ]; then
		LOG_COUNT=$(find "$LOGS_DIR" -type f | wc -l)
		LOG_SIZE=$(du -sh "$LOGS_DIR" 2>/dev/null | cut -f1)

		log_info "Log directory: $LOGS_DIR"
		log_info "Log files: $LOG_COUNT"
		log_info "Total size: $LOG_SIZE"

		write_report "Log directory: $LOGS_DIR"
		write_report "Log files: $LOG_COUNT"
		write_report "Total size: $LOG_SIZE"

		# List recent log files
		write_report ""
		write_report "Recent log files:"
		find "$LOGS_DIR" -type f -printf "%T@ %p\n" | sort -rn | head -5 | while read timestamp file; do
			filename=$(basename "$file")
			size=$(du -h "$file" | cut -f1)
			write_report "  - $filename ($size)"
		done
	else
		log_warn "Logs directory not found"
		write_report "⚠ Logs directory not found"
	fi

	write_report ""
	echo ""
}

# Check disk space
check_disk_space() {
	log_section "Checking Disk Space"
	write_report "DISK SPACE"
	write_report "$(printf '%.s-' {1..60})"

	# Get disk usage for project directory
	PROJECT_SIZE=$(du -sh "$PROJECT_DIR" 2>/dev/null | cut -f1)
	log_info "Project size: $PROJECT_SIZE"
	write_report "Project size: $PROJECT_SIZE"

	# Get available space
	AVAILABLE=$(df -h "$PROJECT_DIR" | tail -1 | awk '{print $4}')
	log_info "Available space: $AVAILABLE"
	write_report "Available space: $AVAILABLE"

	write_report ""
	echo ""
}

# Performance diagnostics
check_performance() {
	log_section "Performance Diagnostics"
	write_report "PERFORMANCE"
	write_report "$(printf '%.s-' {1..60})"

	# Load average
	if [ -f /proc/loadavg ]; then
		LOAD_AVG=$(cat /proc/loadavg | cut -d' ' -f1-3)
		log_info "Load average: $LOAD_AVG"
		write_report "Load average: $LOAD_AVG"
	fi

	# Memory usage
	if command -v free &> /dev/null; then
		MEM_INFO=$(free -h | grep "Mem:" | awk '{print "Used: "$3" / Total: "$2" ("$3/$2*100"%)"}')
		log_info "Memory: $MEM_INFO"
		write_report "Memory: $MEM_INFO"
	fi

	write_report ""
	echo ""
}

# Generate summary
generate_summary() {
	log_section "Diagnostic Summary"
	write_report "SUMMARY"
	write_report "$(printf '%.s-' {1..60})"

	echo ""
	log_success "Diagnostic scan complete"
	log_info "Report saved to: $REPORT_FILE"
	echo ""

	write_report "Diagnostic scan completed successfully"
	write_report "Report generated: $(date)"
}

# Main
main() {
	print_banner
	init_report

	check_system_info
	check_dependencies
	check_project_structure
	check_processes
	check_network
	check_logs
	check_disk_space
	check_performance
	generate_summary

	echo ""
	log_info "To view the full report, run:"
	echo -e "  ${CYAN}cat $REPORT_FILE${NC}"
	echo ""
}

# Run
main
