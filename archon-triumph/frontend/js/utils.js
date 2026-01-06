/**
 * ARCHON TRIUMPH - Utility Functions
 */

const Utils = {
	/**
	 * Format timestamp to readable string
	 */
	formatTime(date = new Date()) {
		return date.toLocaleTimeString('en-US', {
			hour12: false,
			hour: '2-digit',
			minute: '2-digit',
			second: '2-digit'
		});
	},

	/**
	 * Format date to readable string
	 */
	formatDate(date = new Date()) {
		return date.toLocaleDateString('en-US', {
			year: 'numeric',
			month: 'short',
			day: 'numeric'
		});
	},

	/**
	 * Format duration in milliseconds to readable string
	 */
	formatDuration(ms) {
		const seconds = Math.floor(ms / 1000);
		const minutes = Math.floor(seconds / 60);
		const hours = Math.floor(minutes / 60);
		const days = Math.floor(hours / 24);

		if (days > 0) return `${days}d ${hours % 24}h`;
		if (hours > 0) return `${hours}h ${minutes % 60}m`;
		if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
		return `${seconds}s`;
	},

	/**
	 * Format bytes to readable string
	 */
	formatBytes(bytes) {
		if (bytes === 0) return '0 B';
		const k = 1024;
		const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
		const i = Math.floor(Math.log(bytes) / Math.log(k));
		return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
	},

	/**
	 * Debounce function
	 */
	debounce(func, wait) {
		let timeout;
		return function executedFunction(...args) {
			const later = () => {
				clearTimeout(timeout);
				func(...args);
			};
			clearTimeout(timeout);
			timeout = setTimeout(later, wait);
		};
	},

	/**
	 * Throttle function
	 */
	throttle(func, limit) {
		let inThrottle;
		return function(...args) {
			if (!inThrottle) {
				func.apply(this, args);
				inThrottle = true;
				setTimeout(() => inThrottle = false, limit);
			}
		};
	},

	/**
	 * Deep clone object
	 */
	deepClone(obj) {
		return JSON.parse(JSON.stringify(obj));
	},

	/**
	 * Check if object is empty
	 */
	isEmpty(obj) {
		return Object.keys(obj).length === 0;
	},

	/**
	 * Safe JSON parse
	 */
	parseJSON(str, fallback = null) {
		try {
			return JSON.parse(str);
		} catch (e) {
			return fallback;
		}
	},

	/**
	 * Safe JSON stringify
	 */
	stringifyJSON(obj, pretty = false) {
		try {
			return JSON.stringify(obj, null, pretty ? 2 : 0);
		} catch (e) {
			return '';
		}
	},

	/**
	 * Generate random ID
	 */
	generateId() {
		return Date.now().toString(36) + Math.random().toString(36).substr(2);
	},

	/**
	 * Sleep/delay function
	 */
	sleep(ms) {
		return new Promise(resolve => setTimeout(resolve, ms));
	},

	/**
	 * Retry function with exponential backoff
	 */
	async retry(fn, maxAttempts = 3, delay = 1000) {
		for (let attempt = 1; attempt <= maxAttempts; attempt++) {
			try {
				return await fn();
			} catch (error) {
				if (attempt === maxAttempts) throw error;
				await this.sleep(delay * Math.pow(2, attempt - 1));
			}
		}
	},

	/**
	 * Validate JSON string
	 */
	isValidJSON(str) {
		try {
			JSON.parse(str);
			return true;
		} catch (e) {
			return false;
		}
	},

	/**
	 * Escape HTML
	 */
	escapeHTML(str) {
		const div = document.createElement('div');
		div.textContent = str;
		return div.innerHTML;
	},

	/**
	 * Get element by ID (shorthand)
	 */
	$(id) {
		return document.getElementById(id);
	},

	/**
	 * Query selector (shorthand)
	 */
	$$(selector, context = document) {
		return context.querySelectorAll(selector);
	}
};

// Make Utils globally available
window.Utils = Utils;
