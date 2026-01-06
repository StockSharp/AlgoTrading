/**
 * ARCHON TRIUMPH - UI Management
 */

const UI = {
	/**
	 * Initialize UI components
	 */
	init() {
		this.setupNavigation();
		this.setupWindowControls();
		this.setupTheme();
	},

	/**
	 * Setup navigation between panels
	 */
	setupNavigation() {
		const navItems = document.querySelectorAll('.nav-item');
		const panels = document.querySelectorAll('.panel');

		navItems.forEach(item => {
			item.addEventListener('click', () => {
				const panelId = item.getAttribute('data-panel');

				// Update nav items
				navItems.forEach(nav => nav.classList.remove('active'));
				item.classList.add('active');

				// Update panels
				panels.forEach(panel => panel.classList.remove('active'));
				const targetPanel = document.getElementById(`panel-${panelId}`);
				if (targetPanel) {
					targetPanel.classList.add('active');
				}
			});
		});
	},

	/**
	 * Setup window controls
	 */
	setupWindowControls() {
		const btnMinimize = Utils.$('btn-minimize');
		const btnMaximize = Utils.$('btn-maximize');
		const btnClose = Utils.$('btn-close');

		if (btnMinimize) {
			btnMinimize.addEventListener('click', () => {
				window.archon.app.minimize();
			});
		}

		if (btnMaximize) {
			btnMaximize.addEventListener('click', () => {
				window.archon.app.maximize();
			});
		}

		if (btnClose) {
			btnClose.addEventListener('click', () => {
				window.archon.app.quit();
			});
		}
	},

	/**
	 * Setup theme management
	 */
	setupTheme() {
		const themeSelect = Utils.$('setting-theme');
		if (themeSelect) {
			themeSelect.addEventListener('change', (e) => {
				this.setTheme(e.target.value);
			});

			// Load saved theme
			window.archon.store.get('theme', 'dark').then(theme => {
				this.setTheme(theme);
				themeSelect.value = theme;
			});
		}
	},

	/**
	 * Set application theme
	 */
	setTheme(theme) {
		document.body.setAttribute('data-theme', theme);
		window.archon.store.set('theme', theme);
	},

	/**
	 * Show loading screen
	 */
	showLoading(message = 'Loading...') {
		const loadingScreen = Utils.$('loading-screen');
		const loadingMessage = loadingScreen.querySelector('.loading-message');

		if (loadingMessage) {
			loadingMessage.textContent = message;
		}

		loadingScreen.classList.remove('hidden');
	},

	/**
	 * Hide loading screen
	 */
	hideLoading() {
		const loadingScreen = Utils.$('loading-screen');
		const app = Utils.$('app');

		loadingScreen.classList.add('hidden');
		app.classList.remove('hidden');
	},

	/**
	 * Update backend status indicator
	 */
	updateBackendStatus(status) {
		const statusDot = Utils.$('backend-status');
		const statusText = Utils.$('backend-status-text');

		if (status.isRunning) {
			statusDot.classList.add('online');
			statusDot.classList.remove('offline');
			statusText.textContent = 'Backend Online';
		} else {
			statusDot.classList.add('offline');
			statusDot.classList.remove('online');
			statusText.textContent = 'Backend Offline';
		}
	},

	/**
	 * Update WebSocket status indicator
	 */
	updateWSStatus(connected) {
		const wsIndicator = Utils.$('ws-status');

		if (connected) {
			wsIndicator.classList.add('connected');
		} else {
			wsIndicator.classList.remove('connected');
		}
	},

	/**
	 * Show toast notification
	 */
	showToast(title, message, type = 'info', duration = 3000) {
		const container = Utils.$('toast-container');
		const toast = document.createElement('div');
		toast.className = `toast ${type} animate-slide-in-up`;

		const icons = {
			success: '✓',
			error: '✗',
			warning: '⚠',
			info: 'ℹ'
		};

		toast.innerHTML = `
			<span class="toast-icon">${icons[type] || icons.info}</span>
			<div class="toast-content">
				<div class="toast-title">${Utils.escapeHTML(title)}</div>
				<div class="toast-message">${Utils.escapeHTML(message)}</div>
			</div>
			<button class="toast-close">×</button>
		`;

		container.appendChild(toast);

		// Close button
		const closeBtn = toast.querySelector('.toast-close');
		closeBtn.addEventListener('click', () => {
			this.removeToast(toast);
		});

		// Auto remove
		if (duration > 0) {
			setTimeout(() => {
				this.removeToast(toast);
			}, duration);
		}
	},

	/**
	 * Remove toast notification
	 */
	removeToast(toast) {
		toast.classList.add('animate-fade-out');
		setTimeout(() => {
			toast.remove();
		}, 300);
	},

	/**
	 * Add log entry
	 */
	addLog(message, level = 'INFO') {
		const logDisplay = Utils.$('log-display');
		const entry = document.createElement('div');
		entry.className = 'log-entry';

		const timestamp = Utils.formatTime();

		entry.innerHTML = `
			<span class="log-timestamp">${timestamp}</span>
			<span class="log-level ${level}">${level}</span>
			<span class="log-message">${Utils.escapeHTML(message)}</span>
		`;

		logDisplay.appendChild(entry);
		logDisplay.scrollTop = logDisplay.scrollHeight;

		// Keep only last 1000 entries
		while (logDisplay.children.length > 1000) {
			logDisplay.removeChild(logDisplay.firstChild);
		}
	},

	/**
	 * Clear logs
	 */
	clearLogs() {
		const logDisplay = Utils.$('log-display');
		logDisplay.innerHTML = '';
		this.addLog('Logs cleared', 'INFO');
	},

	/**
	 * Update metrics display
	 */
	updateMetrics(metrics) {
		const metricUptime = Utils.$('metric-uptime');
		const metricConnections = Utils.$('metric-connections');
		const metricBackend = Utils.$('metric-backend');
		const metricsDisplay = Utils.$('metrics-display');

		if (metricUptime && metrics.uptime_seconds !== undefined) {
			metricUptime.textContent = Utils.formatDuration(metrics.uptime_seconds * 1000);
		}

		if (metricConnections && metrics.connected_clients !== undefined) {
			metricConnections.textContent = metrics.connected_clients;
		}

		if (metricBackend && metrics.status) {
			metricBackend.textContent = metrics.status;
		}

		if (metricsDisplay && metrics.metrics) {
			metricsDisplay.innerHTML = `<pre>${Utils.stringifyJSON(metrics.metrics, true)}</pre>`;
		}
	},

	/**
	 * Update data display
	 */
	updateDataDisplay(data) {
		const dataDisplay = Utils.$('data-display');
		if (dataDisplay) {
			dataDisplay.innerHTML = `<pre>${Utils.stringifyJSON(data, true)}</pre>`;
		}
	},

	/**
	 * Update command result
	 */
	updateCommandResult(result, isError = false) {
		const commandResult = Utils.$('command-result');
		if (commandResult) {
			commandResult.style.borderColor = isError ? 'var(--color-error)' : 'var(--color-success)';
			commandResult.textContent = Utils.stringifyJSON(result, true);
		}
	},

	/**
	 * Show error dialog
	 */
	async showError(title, message) {
		await window.archon.dialog.showErrorBox(title, message);
	},

	/**
	 * Show confirmation dialog
	 */
	async confirm(title, message) {
		return await window.archon.dialog.confirm(title, message);
	},

	/**
	 * Disable button
	 */
	disableButton(buttonId) {
		const btn = Utils.$(buttonId);
		if (btn) {
			btn.disabled = true;
			btn.style.opacity = '0.5';
			btn.style.cursor = 'not-allowed';
		}
	},

	/**
	 * Enable button
	 */
	enableButton(buttonId) {
		const btn = Utils.$(buttonId);
		if (btn) {
			btn.disabled = false;
			btn.style.opacity = '1';
			btn.style.cursor = 'pointer';
		}
	},

	/**
	 * Set loading state on button
	 */
	setButtonLoading(buttonId, loading = true) {
		const btn = Utils.$(buttonId);
		if (!btn) return;

		if (loading) {
			btn.dataset.originalText = btn.textContent;
			btn.textContent = 'Loading...';
			btn.disabled = true;
		} else {
			btn.textContent = btn.dataset.originalText || btn.textContent;
			btn.disabled = false;
		}
	}
};

// Make UI globally available
window.UI = UI;
