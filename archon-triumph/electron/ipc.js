/**
 * ARCHON TRIUMPH - IPC Handlers
 * Inter-process communication handlers for Electron
 */

const { shell, dialog, Notification } = require('electron');
const fs = require('fs').promises;
const path = require('path');
const os = require('os');
const http = require('http');
const WebSocket = require('ws');

class IPC {
	constructor(backendManager) {
		this.backendManager = backendManager;
		this.ws = null;
		this.store = new Map(); // Simple in-memory store
		this.storeFile = path.join(__dirname, '../.archon-store.json');
		this.loadStore();
	}

	/**
	 * Setup all IPC handlers
	 */
	setupHandlers(ipcMain, mainWindow) {
		this.mainWindow = mainWindow;

		// Backend handlers
		this.setupBackendHandlers(ipcMain);

		// WebSocket handlers
		this.setupWebSocketHandlers(ipcMain);

		// App handlers
		this.setupAppHandlers(ipcMain);

		// File handlers
		this.setupFileHandlers(ipcMain);

		// Log handlers
		this.setupLogHandlers(ipcMain);

		// Notification handlers
		this.setupNotificationHandlers(ipcMain);

		// Store handlers
		this.setupStoreHandlers(ipcMain);

		// Dialog handlers
		this.setupDialogHandlers(ipcMain);

		// System handlers
		this.setupSystemHandlers(ipcMain);

		console.log('IPC handlers registered');
	}

	/**
	 * Backend-related handlers
	 */
	setupBackendHandlers(ipcMain) {
		ipcMain.handle('backend:status', async () => {
			return this.backendManager.getStatus();
		});

		ipcMain.handle('backend:restart', async () => {
			await this.backendManager.restart();
			return { success: true };
		});

		ipcMain.handle('backend:execute', async (event, { command, parameters }) => {
			try {
				const result = await this.makeBackendRequest('/execute', {
					method: 'POST',
					body: JSON.stringify({ command, parameters })
				});
				return result;
			} catch (error) {
				throw new Error(`Failed to execute command: ${error.message}`);
			}
		});

		ipcMain.handle('backend:request', async (event, { endpoint, options }) => {
			try {
				return await this.makeBackendRequest(endpoint, options);
			} catch (error) {
				throw new Error(`Backend request failed: ${error.message}`);
			}
		});
	}

	/**
	 * WebSocket-related handlers
	 */
	setupWebSocketHandlers(ipcMain) {
		ipcMain.handle('ws:connect', async () => {
			if (this.ws && this.ws.readyState === WebSocket.OPEN) {
				return { success: true, message: 'Already connected' };
			}

			const status = this.backendManager.getStatus();
			const wsUrl = `ws://${status.url.split('//')[1]}/ws`;

			return new Promise((resolve, reject) => {
				this.ws = new WebSocket(wsUrl);

				this.ws.on('open', () => {
					console.log('WebSocket connected');
					this.mainWindow.webContents.send('ws:status', { connected: true });
					resolve({ success: true, message: 'Connected' });
				});

				this.ws.on('message', (data) => {
					try {
						const message = JSON.parse(data);
						this.mainWindow.webContents.send('ws:message', message);
					} catch (error) {
						console.error('Failed to parse WebSocket message:', error);
					}
				});

				this.ws.on('error', (error) => {
					console.error('WebSocket error:', error);
					this.mainWindow.webContents.send('ws:status', {
						connected: false,
						error: error.message
					});
				});

				this.ws.on('close', () => {
					console.log('WebSocket disconnected');
					this.mainWindow.webContents.send('ws:status', { connected: false });
				});

				setTimeout(() => {
					if (this.ws.readyState !== WebSocket.OPEN) {
						reject(new Error('WebSocket connection timeout'));
					}
				}, 5000);
			});
		});

		ipcMain.handle('ws:disconnect', async () => {
			if (this.ws) {
				this.ws.close();
				this.ws = null;
			}
			return { success: true };
		});

		ipcMain.handle('ws:send', async (event, message) => {
			if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
				throw new Error('WebSocket not connected');
			}

			this.ws.send(JSON.stringify(message));
			return { success: true };
		});
	}

	/**
	 * App-related handlers
	 */
	setupAppHandlers(ipcMain) {
		ipcMain.handle('app:version', () => {
			return require('../package.json').version;
		});

		ipcMain.handle('app:info', () => {
			return {
				name: 'ARCHON TRIUMPH',
				version: require('../package.json').version,
				electron: process.versions.electron,
				chrome: process.versions.chrome,
				node: process.versions.node,
				platform: process.platform
			};
		});

		ipcMain.handle('app:quit', async () => {
			require('electron').app.quit();
		});

		ipcMain.handle('app:minimize', () => {
			this.mainWindow.minimize();
		});

		ipcMain.handle('app:maximize', () => {
			if (this.mainWindow.isMaximized()) {
				this.mainWindow.unmaximize();
			} else {
				this.mainWindow.maximize();
			}
		});

		ipcMain.handle('app:fullscreen', () => {
			const isFullScreen = this.mainWindow.isFullScreen();
			this.mainWindow.setFullScreen(!isFullScreen);
		});

		ipcMain.handle('app:open-external', async (event, url) => {
			await shell.openExternal(url);
		});
	}

	/**
	 * File-related handlers
	 */
	setupFileHandlers(ipcMain) {
		ipcMain.handle('files:read', async (event, filePath) => {
			try {
				const data = await fs.readFile(filePath, 'utf8');
				return { success: true, data };
			} catch (error) {
				throw new Error(`Failed to read file: ${error.message}`);
			}
		});

		ipcMain.handle('files:write', async (event, { filePath, data }) => {
			try {
				await fs.writeFile(filePath, data, 'utf8');
				return { success: true };
			} catch (error) {
				throw new Error(`Failed to write file: ${error.message}`);
			}
		});

		ipcMain.handle('files:select', async (event, options) => {
			const result = await dialog.showOpenDialog(this.mainWindow, {
				properties: ['openFile'],
				...options
			});
			return result.filePaths[0] || null;
		});

		ipcMain.handle('files:select-directory', async (event, options) => {
			const result = await dialog.showOpenDialog(this.mainWindow, {
				properties: ['openDirectory'],
				...options
			});
			return result.filePaths[0] || null;
		});

		ipcMain.handle('files:save', async (event, options) => {
			const result = await dialog.showSaveDialog(this.mainWindow, options);
			return result.filePath || null;
		});
	}

	/**
	 * Logging handlers
	 */
	setupLogHandlers(ipcMain) {
		const logToFile = async (level, message, args) => {
			const logDir = path.join(__dirname, '../logs');
			const logFile = path.join(logDir, `renderer_${new Date().toISOString().split('T')[0]}.log`);

			const logEntry = `[${new Date().toISOString()}] [${level}] ${message}${args.length ? ' ' + JSON.stringify(args) : ''}\n`;

			try {
				await fs.appendFile(logFile, logEntry);
			} catch (error) {
				console.error('Failed to write log:', error);
			}
		};

		ipcMain.handle('log:info', async (event, { message, args }) => {
			console.log(`[Renderer] ${message}`, ...args);
			await logToFile('INFO', message, args);
		});

		ipcMain.handle('log:warn', async (event, { message, args }) => {
			console.warn(`[Renderer] ${message}`, ...args);
			await logToFile('WARN', message, args);
		});

		ipcMain.handle('log:error', async (event, { message, args }) => {
			console.error(`[Renderer] ${message}`, ...args);
			await logToFile('ERROR', message, args);
		});

		ipcMain.handle('log:debug', async (event, { message, args }) => {
			console.debug(`[Renderer] ${message}`, ...args);
			await logToFile('DEBUG', message, args);
		});
	}

	/**
	 * Notification handlers
	 */
	setupNotificationHandlers(ipcMain) {
		ipcMain.handle('notify:show', async (event, options) => {
			new Notification(options).show();
		});

		ipcMain.handle('notify:error', async (event, { title, message }) => {
			new Notification({
				title,
				body: message,
				urgency: 'critical'
			}).show();
		});

		ipcMain.handle('notify:success', async (event, { title, message }) => {
			new Notification({
				title,
				body: message,
				urgency: 'normal'
			}).show();
		});

		ipcMain.handle('notify:warning', async (event, { title, message }) => {
			new Notification({
				title,
				body: message,
				urgency: 'normal'
			}).show();
		});
	}

	/**
	 * Store handlers
	 */
	setupStoreHandlers(ipcMain) {
		ipcMain.handle('store:get', async (event, { key, defaultValue }) => {
			return this.store.get(key) || defaultValue;
		});

		ipcMain.handle('store:set', async (event, { key, value }) => {
			this.store.set(key, value);
			await this.saveStore();
			return { success: true };
		});

		ipcMain.handle('store:delete', async (event, key) => {
			this.store.delete(key);
			await this.saveStore();
			return { success: true };
		});

		ipcMain.handle('store:clear', async () => {
			this.store.clear();
			await this.saveStore();
			return { success: true };
		});

		ipcMain.handle('store:keys', async () => {
			return Array.from(this.store.keys());
		});
	}

	/**
	 * Dialog handlers
	 */
	setupDialogHandlers(ipcMain) {
		ipcMain.handle('dialog:message', async (event, options) => {
			const result = await dialog.showMessageBox(this.mainWindow, options);
			return result;
		});

		ipcMain.handle('dialog:error', async (event, { title, content }) => {
			dialog.showErrorBox(title, content);
		});

		ipcMain.handle('dialog:confirm', async (event, { title, message }) => {
			const result = await dialog.showMessageBox(this.mainWindow, {
				type: 'question',
				buttons: ['Cancel', 'OK'],
				defaultId: 1,
				title,
				message
			});
			return result.response === 1;
		});
	}

	/**
	 * System handlers
	 */
	setupSystemHandlers(ipcMain) {
		ipcMain.handle('system:info', async () => {
			return {
				platform: os.platform(),
				arch: os.arch(),
				release: os.release(),
				hostname: os.hostname(),
				cpus: os.cpus().length,
				totalMemory: os.totalmem(),
				freeMemory: os.freemem(),
				uptime: os.uptime()
			};
		});

		ipcMain.handle('system:platform', () => {
			return process.platform;
		});

		ipcMain.handle('system:metrics', async () => {
			const processMemory = process.memoryUsage();
			return {
				memory: {
					rss: processMemory.rss,
					heapTotal: processMemory.heapTotal,
					heapUsed: processMemory.heapUsed,
					external: processMemory.external
				},
				cpu: process.cpuUsage(),
				uptime: process.uptime()
			};
		});
	}

	/**
	 * Make HTTP request to backend
	 */
	async makeBackendRequest(endpoint, options = {}) {
		const status = this.backendManager.getStatus();
		const url = `${status.url}${endpoint}`;

		return new Promise((resolve, reject) => {
			const urlObj = new URL(url);
			const requestOptions = {
				hostname: urlObj.hostname,
				port: urlObj.port,
				path: urlObj.pathname + urlObj.search,
				method: options.method || 'GET',
				headers: {
					'Content-Type': 'application/json',
					...options.headers
				}
			};

			const req = http.request(requestOptions, (res) => {
				let data = '';
				res.on('data', chunk => data += chunk);
				res.on('end', () => {
					try {
						const result = JSON.parse(data);
						resolve(result);
					} catch {
						resolve(data);
					}
				});
			});

			req.on('error', (error) => {
				reject(error);
			});

			if (options.body) {
				req.write(options.body);
			}

			req.end();
		});
	}

	/**
	 * Load store from file
	 */
	async loadStore() {
		try {
			const data = await fs.readFile(this.storeFile, 'utf8');
			const obj = JSON.parse(data);
			this.store = new Map(Object.entries(obj));
			console.log('Store loaded');
		} catch (error) {
			// File doesn't exist or is invalid, start with empty store
			this.store = new Map();
		}
	}

	/**
	 * Save store to file
	 */
	async saveStore() {
		try {
			const obj = Object.fromEntries(this.store);
			await fs.writeFile(this.storeFile, JSON.stringify(obj, null, 2));
		} catch (error) {
			console.error('Failed to save store:', error);
		}
	}
}

module.exports = IPC;
