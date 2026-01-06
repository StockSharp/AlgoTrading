/**
 * ARCHON TRIUMPH - WebSocket Communication
 */

const WebSocketManager = {
	connected: false,

	/**
	 * Initialize WebSocket
	 */
	init() {
		this.setupListeners();

		// Check auto-connect setting
		window.archon.store.get('auto-ws', false).then(autoConnect => {
			if (autoConnect) {
				setTimeout(() => this.connect(), 2000);
			}
		});
	},

	/**
	 * Setup WebSocket event listeners
	 */
	setupListeners() {
		// Listen for WebSocket messages
		window.archon.websocket.onMessage((data) => {
			this.handleMessage(data);
		});

		// Listen for WebSocket status changes
		window.archon.websocket.onStatus((status) => {
			this.handleStatusChange(status);
		});
	},

	/**
	 * Connect to WebSocket
	 */
	async connect() {
		try {
			UI.addLog('Connecting to WebSocket...', 'INFO');
			UI.showToast('WebSocket', 'Connecting...', 'info', 2000);

			const result = await window.archon.websocket.connect();

			if (result.success) {
				this.connected = true;
				UI.updateWSStatus(true);
				UI.addLog('WebSocket connected', 'SUCCESS');
				UI.showToast('WebSocket', 'Connected successfully', 'success');

				// Send a ping to test connection
				this.sendPing();
			}
		} catch (error) {
			console.error('WebSocket connection failed:', error);
			UI.addLog(`WebSocket connection failed: ${error.message}`, 'ERROR');
			UI.showToast('Error', 'WebSocket connection failed', 'error');
			this.connected = false;
			UI.updateWSStatus(false);
		}
	},

	/**
	 * Disconnect from WebSocket
	 */
	async disconnect() {
		try {
			UI.addLog('Disconnecting from WebSocket...', 'INFO');

			await window.archon.websocket.disconnect();

			this.connected = false;
			UI.updateWSStatus(false);
			UI.addLog('WebSocket disconnected', 'INFO');
			UI.showToast('WebSocket', 'Disconnected', 'info');
		} catch (error) {
			console.error('WebSocket disconnect failed:', error);
			UI.addLog(`WebSocket disconnect failed: ${error.message}`, 'ERROR');
		}
	},

	/**
	 * Send message through WebSocket
	 */
	async send(message) {
		if (!this.connected) {
			throw new Error('WebSocket not connected');
		}

		try {
			await window.archon.websocket.send(message);
			UI.addLog(`WS Sent: ${JSON.stringify(message)}`, 'INFO');
		} catch (error) {
			console.error('Failed to send WebSocket message:', error);
			UI.addLog(`WS Send failed: ${error.message}`, 'ERROR');
			throw error;
		}
	},

	/**
	 * Send ping message
	 */
	async sendPing() {
		try {
			await this.send({ type: 'ping' });
		} catch (error) {
			console.error('Failed to send ping:', error);
		}
	},

	/**
	 * Execute command through WebSocket
	 */
	async executeCommand(command, parameters = {}) {
		try {
			await this.send({
				type: 'command',
				command: command,
				parameters: parameters
			});

			UI.addLog(`WS Command: ${command}`, 'INFO');
		} catch (error) {
			console.error('Failed to execute command via WebSocket:', error);
			UI.addLog(`WS Command failed: ${error.message}`, 'ERROR');
			throw error;
		}
	},

	/**
	 * Handle incoming WebSocket message
	 */
	handleMessage(data) {
		console.log('WebSocket message received:', data);
		UI.addLog(`WS Received: ${data.type || 'unknown'}`, 'INFO');

		switch (data.type) {
			case 'connection':
				UI.addLog(`WebSocket: ${data.message}`, 'SUCCESS');
				break;

			case 'pong':
				UI.addLog('WebSocket: Pong received', 'INFO');
				break;

			case 'command_response':
				UI.addLog('WebSocket: Command response received', 'SUCCESS');
				if (data.data) {
					UI.updateCommandResult(data.data, false);
				}
				break;

			case 'error':
				UI.addLog(`WebSocket Error: ${data.message}`, 'ERROR');
				UI.showToast('WebSocket Error', data.message, 'error');
				break;

			case 'echo':
				UI.addLog('WebSocket: Echo received', 'INFO');
				break;

			default:
				UI.addLog(`WebSocket: Unknown message type: ${data.type}`, 'WARN');
		}
	},

	/**
	 * Handle WebSocket status change
	 */
	handleStatusChange(status) {
		console.log('WebSocket status changed:', status);

		if (status.connected) {
			this.connected = true;
			UI.updateWSStatus(true);
		} else {
			this.connected = false;
			UI.updateWSStatus(false);

			if (status.error) {
				UI.addLog(`WebSocket Error: ${status.error}`, 'ERROR');
			} else {
				UI.addLog('WebSocket disconnected', 'INFO');
			}
		}
	},

	/**
	 * Get connection status
	 */
	isConnected() {
		return this.connected;
	}
};

// Make WebSocketManager globally available
window.WebSocketManager = WebSocketManager;
