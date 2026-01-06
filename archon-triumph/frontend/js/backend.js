/**
 * ARCHON TRIUMPH - Backend Communication
 */

const Backend = {
	/**
	 * Initialize backend communication
	 */
	init() {
		this.startStatusPolling();
	},

	/**
	 * Get backend status
	 */
	async getStatus() {
		try {
			const status = await window.archon.backend.getStatus();
			UI.updateBackendStatus(status);
			return status;
		} catch (error) {
			console.error('Failed to get backend status:', error);
			UI.addLog(`Failed to get backend status: ${error.message}`, 'ERROR');
			return null;
		}
	},

	/**
	 * Start polling backend status
	 */
	startStatusPolling() {
		// Poll every 5 seconds
		this.statusInterval = setInterval(async () => {
			await this.getStatus();
		}, 5000);

		// Initial check
		this.getStatus();
	},

	/**
	 * Stop polling backend status
	 */
	stopStatusPolling() {
		if (this.statusInterval) {
			clearInterval(this.statusInterval);
		}
	},

	/**
	 * Restart backend
	 */
	async restart() {
		try {
			UI.addLog('Restarting backend...', 'INFO');
			UI.showToast('Backend', 'Restarting backend server...', 'info');

			await window.archon.backend.restart();

			UI.addLog('Backend restarted successfully', 'SUCCESS');
			UI.showToast('Backend', 'Backend restarted successfully', 'success');

			// Refresh status
			setTimeout(() => this.getStatus(), 2000);
		} catch (error) {
			console.error('Failed to restart backend:', error);
			UI.addLog(`Failed to restart backend: ${error.message}`, 'ERROR');
			UI.showToast('Error', 'Failed to restart backend', 'error');
		}
	},

	/**
	 * Execute command on backend
	 */
	async executeCommand(command, parameters = {}) {
		try {
			UI.addLog(`Executing command: ${command}`, 'INFO');

			const result = await window.archon.backend.executeCommand(command, parameters);

			UI.addLog(`Command executed: ${command}`, 'SUCCESS');
			return result;
		} catch (error) {
			console.error('Command execution failed:', error);
			UI.addLog(`Command failed: ${error.message}`, 'ERROR');
			throw error;
		}
	},

	/**
	 * Make HTTP request to backend
	 */
	async request(endpoint, options = {}) {
		try {
			const result = await window.archon.backend.request(endpoint, options);
			return result;
		} catch (error) {
			console.error('Backend request failed:', error);
			throw error;
		}
	},

	/**
	 * Get backend metrics
	 */
	async getMetrics() {
		try {
			const status = await this.request('/status');
			UI.updateMetrics(status);
			return status;
		} catch (error) {
			console.error('Failed to get metrics:', error);
			UI.addLog(`Failed to get metrics: ${error.message}`, 'ERROR');
			return null;
		}
	},

	/**
	 * Start processing
	 */
	async startProcessing() {
		try {
			UI.setButtonLoading('btn-start', true);
			const result = await this.executeCommand('start', {});
			UI.showToast('Processing', 'Processing started', 'success');
			UI.addLog('Processing started', 'SUCCESS');
			return result;
		} catch (error) {
			UI.showToast('Error', 'Failed to start processing', 'error');
			throw error;
		} finally {
			UI.setButtonLoading('btn-start', false);
		}
	},

	/**
	 * Stop processing
	 */
	async stopProcessing() {
		try {
			UI.setButtonLoading('btn-stop', true);
			const result = await this.executeCommand('stop', {});
			UI.showToast('Processing', 'Processing stopped', 'success');
			UI.addLog('Processing stopped', 'SUCCESS');
			return result;
		} catch (error) {
			UI.showToast('Error', 'Failed to stop processing', 'error');
			throw error;
		} finally {
			UI.setButtonLoading('btn-stop', false);
		}
	},

	/**
	 * Get data from backend
	 */
	async getData(type = 'default') {
		try {
			const result = await this.executeCommand('get_data', { type });
			UI.updateDataDisplay(result);
			UI.showToast('Data', 'Data loaded successfully', 'success');
			return result;
		} catch (error) {
			UI.showToast('Error', 'Failed to load data', 'error');
			throw error;
		}
	}
};

// Make Backend globally available
window.Backend = Backend;
