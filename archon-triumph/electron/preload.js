/**
 * ARCHON TRIUMPH - Preload Script
 * Secure bridge between renderer and main process
 */

const { contextBridge, ipcRenderer } = require('electron');

/**
 * Expose safe APIs to the renderer process
 */
contextBridge.exposeInMainWorld('archon', {
	/**
	 * Backend API
	 */
	backend: {
		// Get backend status
		getStatus: () => ipcRenderer.invoke('backend:status'),

		// Restart backend
		restart: () => ipcRenderer.invoke('backend:restart'),

		// Execute command
		executeCommand: (command, parameters) =>
			ipcRenderer.invoke('backend:execute', { command, parameters }),

		// Make HTTP request to backend
		request: (endpoint, options) =>
			ipcRenderer.invoke('backend:request', { endpoint, options })
	},

	/**
	 * WebSocket API
	 */
	websocket: {
		// Connect to WebSocket
		connect: () => ipcRenderer.invoke('ws:connect'),

		// Disconnect from WebSocket
		disconnect: () => ipcRenderer.invoke('ws:disconnect'),

		// Send message
		send: (message) => ipcRenderer.invoke('ws:send', message),

		// Listen for messages
		onMessage: (callback) => {
			ipcRenderer.on('ws:message', (event, data) => callback(data));
		},

		// Remove message listener
		offMessage: (callback) => {
			ipcRenderer.removeListener('ws:message', callback);
		},

		// Connection status
		onStatus: (callback) => {
			ipcRenderer.on('ws:status', (event, status) => callback(status));
		}
	},

	/**
	 * Application API
	 */
	app: {
		// Get app version
		getVersion: () => ipcRenderer.invoke('app:version'),

		// Get app info
		getInfo: () => ipcRenderer.invoke('app:info'),

		// Quit application
		quit: () => ipcRenderer.invoke('app:quit'),

		// Minimize window
		minimize: () => ipcRenderer.invoke('app:minimize'),

		// Maximize window
		maximize: () => ipcRenderer.invoke('app:maximize'),

		// Toggle fullscreen
		toggleFullscreen: () => ipcRenderer.invoke('app:fullscreen'),

		// Open external URL
		openExternal: (url) => ipcRenderer.invoke('app:open-external', url)
	},

	/**
	 * File System API (limited access)
	 */
	files: {
		// Read file
		read: (filePath) => ipcRenderer.invoke('files:read', filePath),

		// Write file
		write: (filePath, data) => ipcRenderer.invoke('files:write', { filePath, data }),

		// Select file dialog
		selectFile: (options) => ipcRenderer.invoke('files:select', options),

		// Select directory dialog
		selectDirectory: (options) => ipcRenderer.invoke('files:select-directory', options),

		// Save file dialog
		saveFile: (options) => ipcRenderer.invoke('files:save', options)
	},

	/**
	 * Logging API
	 */
	log: {
		info: (message, ...args) => ipcRenderer.invoke('log:info', { message, args }),
		warn: (message, ...args) => ipcRenderer.invoke('log:warn', { message, args }),
		error: (message, ...args) => ipcRenderer.invoke('log:error', { message, args }),
		debug: (message, ...args) => ipcRenderer.invoke('log:debug', { message, args })
	},

	/**
	 * Notification API
	 */
	notify: {
		// Show notification
		show: (options) => ipcRenderer.invoke('notify:show', options),

		// Show error notification
		error: (title, message) =>
			ipcRenderer.invoke('notify:error', { title, message }),

		// Show success notification
		success: (title, message) =>
			ipcRenderer.invoke('notify:success', { title, message }),

		// Show warning notification
		warning: (title, message) =>
			ipcRenderer.invoke('notify:warning', { title, message })
	},

	/**
	 * Store API (persistent storage)
	 */
	store: {
		// Get value
		get: (key, defaultValue) =>
			ipcRenderer.invoke('store:get', { key, defaultValue }),

		// Set value
		set: (key, value) =>
			ipcRenderer.invoke('store:set', { key, value }),

		// Delete value
		delete: (key) =>
			ipcRenderer.invoke('store:delete', key),

		// Clear all
		clear: () =>
			ipcRenderer.invoke('store:clear'),

		// Get all keys
		keys: () =>
			ipcRenderer.invoke('store:keys')
	},

	/**
	 * Dialog API
	 */
	dialog: {
		// Show message box
		showMessageBox: (options) =>
			ipcRenderer.invoke('dialog:message', options),

		// Show error box
		showErrorBox: (title, content) =>
			ipcRenderer.invoke('dialog:error', { title, content }),

		// Confirm action
		confirm: (title, message) =>
			ipcRenderer.invoke('dialog:confirm', { title, message })
	},

	/**
	 * System API
	 */
	system: {
		// Get system info
		getInfo: () => ipcRenderer.invoke('system:info'),

		// Get platform
		getPlatform: () => ipcRenderer.invoke('system:platform'),

		// Get metrics
		getMetrics: () => ipcRenderer.invoke('system:metrics')
	},

	/**
	 * Event listeners
	 */
	on: (channel, callback) => {
		const validChannels = [
			'app:update',
			'backend:status',
			'ws:message',
			'ws:status',
			'log:message'
		];

		if (validChannels.includes(channel)) {
			ipcRenderer.on(channel, (event, ...args) => callback(...args));
		} else {
			console.warn(`Invalid channel: ${channel}`);
		}
	},

	off: (channel, callback) => {
		ipcRenderer.removeListener(channel, callback);
	},

	/**
	 * One-time event listeners
	 */
	once: (channel, callback) => {
		const validChannels = [
			'app:ready',
			'backend:ready',
			'ws:connected'
		];

		if (validChannels.includes(channel)) {
			ipcRenderer.once(channel, (event, ...args) => callback(...args));
		}
	}
});

/**
 * Console override for better logging
 */
const originalConsole = {
	log: console.log,
	warn: console.warn,
	error: console.error,
	info: console.info,
	debug: console.debug
};

// Keep original console for renderer, but also send to main process
console.log = (...args) => {
	originalConsole.log(...args);
	ipcRenderer.invoke('log:info', { message: args.join(' '), args: [] });
};

console.warn = (...args) => {
	originalConsole.warn(...args);
	ipcRenderer.invoke('log:warn', { message: args.join(' '), args: [] });
};

console.error = (...args) => {
	originalConsole.error(...args);
	ipcRenderer.invoke('log:error', { message: args.join(' '), args: [] });
};

/**
 * Error handler
 */
window.addEventListener('error', (event) => {
	console.error('Unhandled error:', event.error);
	ipcRenderer.invoke('log:error', {
		message: 'Unhandled error in renderer',
		args: [event.error.stack || event.error.toString()]
	});
});

window.addEventListener('unhandledrejection', (event) => {
	console.error('Unhandled promise rejection:', event.reason);
	ipcRenderer.invoke('log:error', {
		message: 'Unhandled rejection in renderer',
		args: [event.reason]
	});
});

console.log('ARCHON TRIUMPH - Preload script loaded');
