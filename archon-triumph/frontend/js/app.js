/**
 * ARCHON TRIUMPH - Main Application
 */

class ArchonApp {
	constructor() {
		this.initialized = false;
	}

	/**
	 * Initialize the application
	 */
	async init() {
		console.log('='.repeat(60));
		console.log('ARCHON TRIUMPH - Initializing');
		console.log('='.repeat(60));

		try {
			// Show loading screen
			UI.showLoading('Initializing application...');

			// Initialize UI
			UI.init();
			UI.addLog('UI initialized', 'SUCCESS');

			// Initialize Backend
			Backend.init();
			UI.addLog('Backend module initialized', 'SUCCESS');

			// Initialize WebSocket
			WebSocketManager.init();
			UI.addLog('WebSocket module initialized', 'SUCCESS');

			// Setup event handlers
			this.setupEventHandlers();
			UI.addLog('Event handlers registered', 'SUCCESS');

			// Load application info
			await this.loadAppInfo();

			// Wait a bit for backend to be ready
			await Utils.sleep(1000);

			// Get initial backend status
			await Backend.getStatus();

			// Get initial metrics
			await Backend.getMetrics();

			// Hide loading screen
			UI.hideLoading();

			this.initialized = true;
			console.log('ARCHON TRIUMPH - Initialization complete');
			UI.addLog('Application initialized successfully', 'SUCCESS');
			UI.showToast('ARCHON TRIUMPH', 'Application ready', 'success');

		} catch (error) {
			console.error('Initialization failed:', error);
			UI.addLog(`Initialization failed: ${error.message}`, 'ERROR');
			UI.showError('Initialization Error', error.message);
		}
	}

	/**
	 * Setup event handlers
	 */
	setupEventHandlers() {
		// Dashboard buttons
		this.setupButton('btn-start', () => this.handleStart());
		this.setupButton('btn-stop', () => this.handleStop());
		this.setupButton('btn-refresh', () => this.handleRefresh());

		// Control panel buttons
		this.setupButton('btn-backend-restart', () => this.handleBackendRestart());
		this.setupButton('btn-ws-connect', () => this.handleWSConnect());
		this.setupButton('btn-ws-disconnect', () => this.handleWSDisconnect());
		this.setupButton('btn-execute-command', () => this.handleExecuteCommand());

		// Data panel buttons
		this.setupButton('btn-load-data', () => this.handleLoadData());
		this.setupButton('btn-export-data', () => this.handleExportData());

		// Logs panel buttons
		this.setupButton('btn-clear-logs', () => UI.clearLogs());

		// Settings
		this.setupAutoWSCheckbox();
	}

	/**
	 * Setup button with click handler
	 */
	setupButton(id, handler) {
		const btn = Utils.$(id);
		if (btn) {
			btn.addEventListener('click', async () => {
				try {
					await handler();
				} catch (error) {
					console.error(`Button ${id} handler error:`, error);
					UI.showToast('Error', error.message, 'error');
				}
			});
		}
	}

	/**
	 * Setup auto-connect WebSocket checkbox
	 */
	setupAutoWSCheckbox() {
		const checkbox = Utils.$('setting-auto-ws');
		if (checkbox) {
			// Load saved value
			window.archon.store.get('auto-ws', false).then(value => {
				checkbox.checked = value;
			});

			// Save on change
			checkbox.addEventListener('change', (e) => {
				window.archon.store.set('auto-ws', e.target.checked);
				UI.addLog(`Auto-connect WebSocket: ${e.target.checked ? 'enabled' : 'disabled'}`, 'INFO');
			});
		}
	}

	/**
	 * Load application info
	 */
	async loadAppInfo() {
		try {
			const info = await window.archon.app.getInfo();
			const appInfoEl = Utils.$('app-info');

			if (appInfoEl) {
				appInfoEl.innerHTML = `
					<p><strong>Name:</strong> ${info.name}</p>
					<p><strong>Version:</strong> ${info.version}</p>
					<p><strong>Platform:</strong> ${info.platform}</p>
					<p><strong>Electron:</strong> ${info.electron}</p>
					<p><strong>Chrome:</strong> ${info.chrome}</p>
					<p><strong>Node:</strong> ${info.node}</p>
				`;
			}

			UI.addLog(`Application: ${info.name} v${info.version}`, 'INFO');
		} catch (error) {
			console.error('Failed to load app info:', error);
		}
	}

	/**
	 * Handle start button
	 */
	async handleStart() {
		UI.addLog('Starting processing...', 'INFO');
		await Backend.startProcessing();
		await Backend.getMetrics();
	}

	/**
	 * Handle stop button
	 */
	async handleStop() {
		UI.addLog('Stopping processing...', 'INFO');
		await Backend.stopProcessing();
		await Backend.getMetrics();
	}

	/**
	 * Handle refresh button
	 */
	async handleRefresh() {
		UI.addLog('Refreshing status...', 'INFO');
		UI.setButtonLoading('btn-refresh', true);

		try {
			await Backend.getStatus();
			await Backend.getMetrics();
			UI.showToast('Status', 'Status refreshed', 'success');
		} finally {
			UI.setButtonLoading('btn-refresh', false);
		}
	}

	/**
	 * Handle backend restart button
	 */
	async handleBackendRestart() {
		const confirmed = await UI.confirm(
			'Restart Backend',
			'Are you sure you want to restart the backend server?'
		);

		if (confirmed) {
			await Backend.restart();
		}
	}

	/**
	 * Handle WebSocket connect button
	 */
	async handleWSConnect() {
		if (WebSocketManager.isConnected()) {
			UI.showToast('WebSocket', 'Already connected', 'info');
			return;
		}

		await WebSocketManager.connect();
	}

	/**
	 * Handle WebSocket disconnect button
	 */
	async handleWSDisconnect() {
		if (!WebSocketManager.isConnected()) {
			UI.showToast('WebSocket', 'Not connected', 'info');
			return;
		}

		await WebSocketManager.disconnect();
	}

	/**
	 * Handle execute command button
	 */
	async handleExecuteCommand() {
		const commandSelect = Utils.$('command-select');
		const paramsInput = Utils.$('command-params');

		const command = commandSelect.value;
		let parameters = {};

		// Parse parameters
		if (paramsInput.value.trim()) {
			try {
				parameters = JSON.parse(paramsInput.value);
			} catch (error) {
				UI.showToast('Error', 'Invalid JSON parameters', 'error');
				UI.updateCommandResult({ error: 'Invalid JSON' }, true);
				return;
			}
		}

		UI.setButtonLoading('btn-execute-command', true);

		try {
			const result = await Backend.executeCommand(command, parameters);
			UI.updateCommandResult(result, false);
			UI.showToast('Command', 'Command executed successfully', 'success');
		} catch (error) {
			UI.updateCommandResult({ error: error.message }, true);
			UI.showToast('Error', 'Command execution failed', 'error');
		} finally {
			UI.setButtonLoading('btn-execute-command', false);
		}
	}

	/**
	 * Handle load data button
	 */
	async handleLoadData() {
		UI.setButtonLoading('btn-load-data', true);

		try {
			await Backend.getData();
		} finally {
			UI.setButtonLoading('btn-load-data', false);
		}
	}

	/**
	 * Handle export data button
	 */
	async handleExportData() {
		try {
			const filePath = await window.archon.files.saveFile({
				title: 'Export Data',
				defaultPath: `archon-data-${Date.now()}.json`,
				filters: [
					{ name: 'JSON', extensions: ['json'] },
					{ name: 'All Files', extensions: ['*'] }
				]
			});

			if (filePath) {
				// Get current data from display
				const dataDisplay = Utils.$('data-display');
				const data = dataDisplay.textContent;

				await window.archon.files.write(filePath, data);
				UI.showToast('Export', 'Data exported successfully', 'success');
				UI.addLog(`Data exported to: ${filePath}`, 'SUCCESS');
			}
		} catch (error) {
			UI.showToast('Error', 'Failed to export data', 'error');
			UI.addLog(`Export failed: ${error.message}`, 'ERROR');
		}
	}
}

// Create and initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
	window.app = new ArchonApp();
	window.app.init();
});

// Handle unload
window.addEventListener('beforeunload', () => {
	if (window.app) {
		Backend.stopStatusPolling();
	}
});

console.log('ARCHON TRIUMPH - Application script loaded');
