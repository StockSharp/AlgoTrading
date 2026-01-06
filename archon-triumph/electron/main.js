/**
 * ARCHON TRIUMPH - Main Electron Process
 * Manages application lifecycle and window creation
 */

const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('path');
const fs = require('fs');
const BackendManager = require('./backend');
const IPC = require('./ipc');

class ArchonTriumph {
	constructor() {
		this.mainWindow = null;
		this.backendManager = new BackendManager();
		this.ipc = new IPC(this.backendManager);
		this.isQuitting = false;
	}

	/**
	 * Initialize the application
	 */
	async initialize() {
		console.log('='.repeat(60));
		console.log('ARCHON TRIUMPH - Initializing');
		console.log('='.repeat(60));

		// Setup application event handlers
		this.setupAppHandlers();

		// Wait for app to be ready
		await app.whenReady();

		// Start backend server
		await this.startBackend();

		// Create main window
		this.createMainWindow();

		// Setup IPC handlers
		this.ipc.setupHandlers(ipcMain, this.mainWindow);

		console.log('ARCHON TRIUMPH - Initialization complete');
	}

	/**
	 * Setup application-level event handlers
	 */
	setupAppHandlers() {
		app.on('window-all-closed', () => {
			if (process.platform !== 'darwin') {
				this.shutdown();
			}
		});

		app.on('activate', () => {
			if (BrowserWindow.getAllWindows().length === 0) {
				this.createMainWindow();
			}
		});

		app.on('before-quit', async (event) => {
			if (!this.isQuitting) {
				event.preventDefault();
				await this.shutdown();
			}
		});

		// Handle uncaught exceptions
		process.on('uncaughtException', (error) => {
			console.error('Uncaught Exception:', error);
			this.logError('Uncaught Exception', error);
		});

		process.on('unhandledRejection', (reason, promise) => {
			console.error('Unhandled Rejection at:', promise, 'reason:', reason);
			this.logError('Unhandled Rejection', reason);
		});
	}

	/**
	 * Create the main application window
	 */
	createMainWindow() {
		console.log('Creating main window...');

		this.mainWindow = new BrowserWindow({
			width: 1400,
			height: 900,
			minWidth: 1000,
			minHeight: 600,
			backgroundColor: '#1a1a1a',
			title: 'ARCHON TRIUMPH',
			icon: path.join(__dirname, '../frontend/assets/icon.png'),
			webPreferences: {
				nodeIntegration: false,
				contextIsolation: true,
				enableRemoteModule: false,
				preload: path.join(__dirname, 'preload.js')
			},
			frame: true,
			show: false // Don't show until ready
		});

		// Load the frontend
		const indexPath = path.join(__dirname, '../frontend/index.html');
		this.mainWindow.loadFile(indexPath);

		// Show window when ready
		this.mainWindow.once('ready-to-show', () => {
			this.mainWindow.show();
			console.log('Main window displayed');
		});

		// Handle window close
		this.mainWindow.on('close', async (event) => {
			if (!this.isQuitting) {
				event.preventDefault();
				const result = await this.confirmQuit();
				if (result) {
					await this.shutdown();
				}
			}
		});

		// Open DevTools in development
		if (process.env.NODE_ENV === 'development') {
			this.mainWindow.webContents.openDevTools();
		}

		// Handle navigation
		this.mainWindow.webContents.on('will-navigate', (event, url) => {
			// Prevent navigation to external URLs
			if (!url.startsWith('file://')) {
				event.preventDefault();
				console.warn('Navigation blocked:', url);
			}
		});

		console.log('Main window created successfully');
	}

	/**
	 * Start the Python backend server
	 */
	async startBackend() {
		console.log('Starting backend server...');
		try {
			await this.backendManager.start();
			console.log('Backend server started successfully');
		} catch (error) {
			console.error('Failed to start backend:', error);
			await this.showError('Backend Startup Failed', error.message);
			throw error;
		}
	}

	/**
	 * Shutdown the application gracefully
	 */
	async shutdown() {
		if (this.isQuitting) return;

		console.log('='.repeat(60));
		console.log('ARCHON TRIUMPH - Shutting down');
		console.log('='.repeat(60));

		this.isQuitting = true;

		try {
			// Stop backend server
			console.log('Stopping backend server...');
			await this.backendManager.stop();
			console.log('Backend server stopped');

			// Close all windows
			BrowserWindow.getAllWindows().forEach(window => {
				window.destroy();
			});

			// Quit application
			app.quit();
		} catch (error) {
			console.error('Error during shutdown:', error);
			app.quit();
		}
	}

	/**
	 * Confirm quit with user
	 */
	async confirmQuit() {
		const result = await dialog.showMessageBox(this.mainWindow, {
			type: 'question',
			buttons: ['Cancel', 'Quit'],
			defaultId: 0,
			title: 'Quit ARCHON TRIUMPH?',
			message: 'Are you sure you want to quit?',
			detail: 'All running processes will be stopped.'
		});

		return result.response === 1;
	}

	/**
	 * Show error dialog
	 */
	async showError(title, message) {
		await dialog.showMessageBox(this.mainWindow, {
			type: 'error',
			title: title,
			message: message,
			buttons: ['OK']
		});
	}

	/**
	 * Log error to file
	 */
	logError(type, error) {
		const logDir = path.join(__dirname, '../logs');
		const logFile = path.join(logDir, `electron_${new Date().toISOString().split('T')[0]}.log`);

		const errorLog = `
[${new Date().toISOString()}] ${type}
${error.stack || error.toString()}
${'='.repeat(60)}
`;

		try {
			if (!fs.existsSync(logDir)) {
				fs.mkdirSync(logDir, { recursive: true });
			}
			fs.appendFileSync(logFile, errorLog);
		} catch (e) {
			console.error('Failed to write error log:', e);
		}
	}
}

// Create and initialize application
const archonTriumph = new ArchonTriumph();

// Start the application
archonTriumph.initialize().catch(error => {
	console.error('Failed to initialize ARCHON TRIUMPH:', error);
	app.quit();
});

// Export for testing
module.exports = ArchonTriumph;
