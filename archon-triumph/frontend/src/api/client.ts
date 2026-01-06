/**
 * ARCHON TRIUMPH - API Client
 * Wrapper around IPC calls for type-safe API access
 */

import type { ArchonAPI } from '@/types/ipc'

/**
 * Get the Archon API instance
 * Throws error if not running in Electron
 */
export function getAPI(): ArchonAPI {
	if (!window.archonAPI) {
		throw new Error('Archon API not available. Are you running in Electron?')
	}
	return window.archonAPI
}

/**
 * API client with typed methods
 */
export const api = {
	// Backend
	backend: {
		async health() {
			return getAPI().backend.health()
		},
		async status() {
			return getAPI().backend.status()
		},
		async request<T>(endpoint: string, options?: RequestInit) {
			return getAPI().backend.request<T>(endpoint, options)
		},
	},

	// Brokers
	brokers: {
		async list() {
			return getAPI().brokers.list()
		},
		async get(brokerId: string) {
			return getAPI().brokers.get(brokerId)
		},
		async create(request: Parameters<ArchonAPI['brokers']['create']>[0]) {
			return getAPI().brokers.create(request)
		},
		async update(brokerId: string, request: Parameters<ArchonAPI['brokers']['update']>[1]) {
			return getAPI().brokers.update(brokerId, request)
		},
		async delete(brokerId: string) {
			return getAPI().brokers.delete(brokerId)
		},
		async connect(request: Parameters<ArchonAPI['brokers']['connect']>[0]) {
			return getAPI().brokers.connect(request)
		},
		async disconnect(request: Parameters<ArchonAPI['brokers']['disconnect']>[0]) {
			return getAPI().brokers.disconnect(request)
		},
	},

	// Plugins
	plugins: {
		async list() {
			return getAPI().plugins.list()
		},
		async get(pluginId: string) {
			return getAPI().plugins.get(pluginId)
		},
		async install(request: Parameters<ArchonAPI['plugins']['install']>[0]) {
			return getAPI().plugins.install(request)
		},
		async uninstall(pluginId: string) {
			return getAPI().plugins.uninstall(pluginId)
		},
		async enable(pluginId: string) {
			return getAPI().plugins.enable(pluginId)
		},
		async disable(pluginId: string) {
			return getAPI().plugins.disable(pluginId)
		},
		async reload(pluginId: string) {
			return getAPI().plugins.reload(pluginId)
		},
		async update(pluginId: string, update: Parameters<ArchonAPI['plugins']['update']>[1]) {
			return getAPI().plugins.update(pluginId, update)
		},
		async execute(request: Parameters<ArchonAPI['plugins']['execute']>[0]) {
			return getAPI().plugins.execute(request)
		},
	},

	// System
	system: {
		async info() {
			return getAPI().system.info()
		},
		async metrics() {
			return getAPI().system.metrics()
		},
		async logs(query: Parameters<ArchonAPI['system']['logs']>[0]) {
			return getAPI().system.logs(query)
		},
		async command(request: Parameters<ArchonAPI['system']['command']>[0]) {
			return getAPI().system.command(request)
		},
		config: {
			async get() {
				return getAPI().system.config.get()
			},
			async update(update: Parameters<ArchonAPI['system']['config']['update']>[0]) {
				return getAPI().system.config.update(update)
			},
		},
	},

	// WebSocket
	websocket: {
		async connect() {
			return getAPI().websocket.connect()
		},
		async disconnect() {
			return getAPI().websocket.disconnect()
		},
		async send(message: Parameters<ArchonAPI['websocket']['send']>[0]) {
			return getAPI().websocket.send(message)
		},
		onMessage(callback: Parameters<ArchonAPI['websocket']['onMessage']>[0]) {
			getAPI().websocket.onMessage(callback)
		},
		offMessage(callback: Parameters<ArchonAPI['websocket']['offMessage']>[0]) {
			getAPI().websocket.offMessage(callback)
		},
		onStatus(callback: Parameters<ArchonAPI['websocket']['onStatus']>[0]) {
			getAPI().websocket.onStatus(callback)
		},
	},

	// App
	app: {
		async version() {
			return getAPI().app.version()
		},
		async info() {
			return getAPI().app.info()
		},
		async quit() {
			return getAPI().app.quit()
		},
		async minimize() {
			return getAPI().app.minimize()
		},
		async maximize() {
			return getAPI().app.maximize()
		},
		async toggleFullscreen() {
			return getAPI().app.toggleFullscreen()
		},
		async openExternal(url: string) {
			return getAPI().app.openExternal(url)
		},
	},

	// Files
	files: {
		async read(filePath: string) {
			return getAPI().files.read(filePath)
		},
		async write(filePath: string, data: string) {
			return getAPI().files.write(filePath, data)
		},
		async selectFile(options?: any) {
			return getAPI().files.selectFile(options)
		},
		async selectDirectory(options?: any) {
			return getAPI().files.selectDirectory(options)
		},
		async saveFile(options?: any) {
			return getAPI().files.saveFile(options)
		},
	},

	// Notifications
	notify: {
		async show(options: Parameters<ArchonAPI['notify']['show']>[0]) {
			return getAPI().notify.show(options)
		},
		async error(title: string, message: string) {
			return getAPI().notify.error(title, message)
		},
		async success(title: string, message: string) {
			return getAPI().notify.success(title, message)
		},
		async warning(title: string, message: string) {
			return getAPI().notify.warning(title, message)
		},
	},

	// Store
	store: {
		async get<T>(key: string, defaultValue?: T) {
			return getAPI().store.get<T>(key, defaultValue)
		},
		async set(key: string, value: any) {
			return getAPI().store.set(key, value)
		},
		async delete(key: string) {
			return getAPI().store.delete(key)
		},
		async clear() {
			return getAPI().store.clear()
		},
		async keys() {
			return getAPI().store.keys()
		},
	},

	// Dialog
	dialog: {
		async showMessageBox(options: any) {
			return getAPI().dialog.showMessageBox(options)
		},
		async showErrorBox(title: string, content: string) {
			return getAPI().dialog.showErrorBox(title, content)
		},
		async confirm(title: string, message: string) {
			return getAPI().dialog.confirm(title, message)
		},
	},

	// Event listeners
	on(channel: string, callback: (...args: any[]) => void) {
		getAPI().on(channel, callback)
	},
	off(channel: string, callback: (...args: any[]) => void) {
		getAPI().off(channel, callback)
	},
	once(channel: string, callback: (...args: any[]) => void) {
		getAPI().once(channel, callback)
	},
}

export default api
