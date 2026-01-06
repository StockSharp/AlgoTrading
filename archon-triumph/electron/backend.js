/**
 * ARCHON TRIUMPH - Backend Process Manager
 * Manages the Python backend server lifecycle
 */

const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');
const http = require('http');

class BackendManager {
	constructor() {
		this.process = null;
		this.port = process.env.ARCHON_PORT || 8000;
		this.host = process.env.ARCHON_HOST || '127.0.0.1';
		this.baseUrl = `http://${this.host}:${this.port}`;
		this.isRunning = false;
		this.startTime = null;
		this.logStream = null;
		this.maxStartupTime = 30000; // 30 seconds
		this.healthCheckInterval = null;
	}

	/**
	 * Start the Python backend server
	 */
	async start() {
		if (this.isRunning) {
			console.log('Backend is already running');
			return;
		}

		console.log('Starting Python backend server...');
		this.startTime = Date.now();

		// Setup logging
		this.setupLogging();

		// Find Python executable
		const pythonCmd = this.findPython();
		if (!pythonCmd) {
			throw new Error('Python 3 not found. Please install Python 3.8 or higher.');
		}

		// Get backend script path
		const backendPath = path.join(__dirname, '../backend/main.py');
		if (!fs.existsSync(backendPath)) {
			throw new Error(`Backend script not found: ${backendPath}`);
		}

		// Check if dependencies are installed
		await this.checkDependencies(pythonCmd);

		// Spawn Python process
		console.log(`Spawning: ${pythonCmd} ${backendPath}`);
		this.process = spawn(pythonCmd, [backendPath], {
			cwd: path.join(__dirname, '../backend'),
			env: {
				...process.env,
				ARCHON_HOST: this.host,
				ARCHON_PORT: this.port.toString(),
				PYTHONUNBUFFERED: '1'
			},
			stdio: ['ignore', 'pipe', 'pipe']
		});

		// Handle process events
		this.setupProcessHandlers();

		// Wait for server to be ready
		await this.waitForReady();

		// Start health monitoring
		this.startHealthMonitoring();

		this.isRunning = true;
		const startupTime = Date.now() - this.startTime;
		console.log(`Backend started successfully in ${startupTime}ms`);
	}

	/**
	 * Find Python executable
	 */
	findPython() {
		const candidates = ['python3', 'python'];

		for (const cmd of candidates) {
			try {
				const result = require('child_process').spawnSync(cmd, ['--version'], {
					encoding: 'utf8',
					timeout: 5000
				});

				if (result.status === 0) {
					const version = result.stdout || result.stderr;
					console.log(`Found Python: ${cmd} - ${version.trim()}`);
					return cmd;
				}
			} catch (e) {
				continue;
			}
		}

		return null;
	}

	/**
	 * Check if Python dependencies are installed
	 */
	async checkDependencies(pythonCmd) {
		const requirementsFile = path.join(__dirname, '../backend/requirements.txt');

		if (!fs.existsSync(requirementsFile)) {
			console.warn('requirements.txt not found, skipping dependency check');
			return;
		}

		console.log('Checking Python dependencies...');

		// Check if FastAPI is installed
		const result = require('child_process').spawnSync(
			pythonCmd,
			['-c', 'import fastapi; import uvicorn'],
			{ encoding: 'utf8', timeout: 5000 }
		);

		if (result.status !== 0) {
			console.log('Dependencies not installed. Installing...');
			await this.installDependencies(pythonCmd, requirementsFile);
		} else {
			console.log('Dependencies are installed');
		}
	}

	/**
	 * Install Python dependencies
	 */
	async installDependencies(pythonCmd, requirementsFile) {
		return new Promise((resolve, reject) => {
			const pip = spawn(pythonCmd, ['-m', 'pip', 'install', '-r', requirementsFile], {
				cwd: path.join(__dirname, '../backend'),
				stdio: 'inherit'
			});

			pip.on('close', (code) => {
				if (code === 0) {
					console.log('Dependencies installed successfully');
					resolve();
				} else {
					reject(new Error(`Failed to install dependencies (exit code ${code})`));
				}
			});

			pip.on('error', (error) => {
				reject(new Error(`Failed to run pip: ${error.message}`));
			});
		});
	}

	/**
	 * Setup logging for backend process
	 */
	setupLogging() {
		const logDir = path.join(__dirname, '../logs');
		if (!fs.existsSync(logDir)) {
			fs.mkdirSync(logDir, { recursive: true });
		}

		const logFile = path.join(logDir, `backend_${new Date().toISOString().split('T')[0]}.log`);
		this.logStream = fs.createWriteStream(logFile, { flags: 'a' });

		console.log(`Backend logs: ${logFile}`);
	}

	/**
	 * Setup process event handlers
	 */
	setupProcessHandlers() {
		this.process.stdout.on('data', (data) => {
			const message = data.toString();
			console.log(`[Backend] ${message.trim()}`);
			if (this.logStream) {
				this.logStream.write(`[STDOUT] ${message}`);
			}
		});

		this.process.stderr.on('data', (data) => {
			const message = data.toString();
			console.error(`[Backend Error] ${message.trim()}`);
			if (this.logStream) {
				this.logStream.write(`[STDERR] ${message}`);
			}
		});

		this.process.on('close', (code) => {
			console.log(`Backend process exited with code ${code}`);
			this.isRunning = false;
			if (this.healthCheckInterval) {
				clearInterval(this.healthCheckInterval);
			}
			if (this.logStream) {
				this.logStream.end();
			}
		});

		this.process.on('error', (error) => {
			console.error('Backend process error:', error);
			this.isRunning = false;
		});
	}

	/**
	 * Wait for backend to be ready
	 */
	async waitForReady() {
		console.log('Waiting for backend to be ready...');
		const startTime = Date.now();

		while (Date.now() - startTime < this.maxStartupTime) {
			try {
				const healthy = await this.healthCheck();
				if (healthy) {
					console.log('Backend is ready!');
					return;
				}
			} catch (error) {
				// Continue waiting
			}

			await this.sleep(500);
		}

		throw new Error('Backend failed to start within timeout period');
	}

	/**
	 * Perform health check
	 */
	async healthCheck() {
		return new Promise((resolve) => {
			const req = http.get(`${this.baseUrl}/health`, (res) => {
				let data = '';
				res.on('data', chunk => data += chunk);
				res.on('end', () => {
					try {
						const json = JSON.parse(data);
						resolve(json.status === 'healthy');
					} catch {
						resolve(false);
					}
				});
			});

			req.on('error', () => resolve(false));
			req.setTimeout(2000, () => {
				req.destroy();
				resolve(false);
			});
		});
	}

	/**
	 * Start health monitoring
	 */
	startHealthMonitoring() {
		this.healthCheckInterval = setInterval(async () => {
			if (!this.isRunning) return;

			const healthy = await this.healthCheck();
			if (!healthy) {
				console.error('Backend health check failed!');
				// Could trigger auto-restart here
			}
		}, 30000); // Check every 30 seconds
	}

	/**
	 * Stop the backend server
	 */
	async stop() {
		if (!this.isRunning || !this.process) {
			console.log('Backend is not running');
			return;
		}

		console.log('Stopping backend server...');

		// Stop health monitoring
		if (this.healthCheckInterval) {
			clearInterval(this.healthCheckInterval);
		}

		return new Promise((resolve) => {
			// Give the process 5 seconds to shut down gracefully
			const timeout = setTimeout(() => {
				console.log('Force killing backend process...');
				this.process.kill('SIGKILL');
			}, 5000);

			this.process.once('close', () => {
				clearTimeout(timeout);
				this.isRunning = false;
				if (this.logStream) {
					this.logStream.end();
				}
				console.log('Backend stopped');
				resolve();
			});

			// Send termination signal
			this.process.kill('SIGTERM');
		});
	}

	/**
	 * Restart the backend server
	 */
	async restart() {
		console.log('Restarting backend server...');
		await this.stop();
		await this.sleep(1000);
		await this.start();
	}

	/**
	 * Get backend status
	 */
	getStatus() {
		return {
			isRunning: this.isRunning,
			pid: this.process ? this.process.pid : null,
			uptime: this.startTime ? Date.now() - this.startTime : 0,
			url: this.baseUrl
		};
	}

	/**
	 * Sleep utility
	 */
	sleep(ms) {
		return new Promise(resolve => setTimeout(resolve, ms));
	}
}

module.exports = BackendManager;
