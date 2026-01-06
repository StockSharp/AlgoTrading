/**
 * ARCHON TRIUMPH - IPC Type Definitions
 * TypeScript types for Electron IPC communication
 */

import type {
	HealthResponse,
	StatusResponse,
	BrokerListResponse,
	BrokerInfo,
	CreateBrokerRequest,
	CreateBrokerResponse,
	UpdateBrokerRequest,
	UpdateBrokerResponse,
	ConnectBrokerRequest,
	ConnectBrokerResponse,
	DisconnectBrokerRequest,
	DisconnectBrokerResponse,
	DeleteBrokerResponse,
	PluginListResponse,
	PluginInfo,
	InstallPluginRequest,
	InstallPluginResponse,
	UninstallPluginResponse,
	PluginActionResponse,
	PluginExecuteRequest,
	PluginExecuteResponse,
	PluginConfigUpdate,
	SystemInfo,
	SystemMetrics,
	LogQuery,
	LogListResponse,
	CommandRequest,
	CommandResponse,
	ConfigResponse,
	ConfigUpdate,
	WebSocketMessage,
} from './api'

/**
 * IPC API exposed to renderer process via preload script
 */
export interface ArchonAPI {
	// Backend communication
	backend: {
		health: () => Promise<HealthResponse>
		status: () => Promise<StatusResponse>
		request: <T = any>(endpoint: string, options?: RequestInit) => Promise<T>
	}

	// Broker management
	brokers: {
		list: () => Promise<BrokerListResponse>
		get: (brokerId: string) => Promise<BrokerInfo>
		create: (request: CreateBrokerRequest) => Promise<CreateBrokerResponse>
		update: (brokerId: string, request: UpdateBrokerRequest) => Promise<UpdateBrokerResponse>
		delete: (brokerId: string) => Promise<DeleteBrokerResponse>
		connect: (request: ConnectBrokerRequest) => Promise<ConnectBrokerResponse>
		disconnect: (request: DisconnectBrokerRequest) => Promise<DisconnectBrokerResponse>
	}

	// Plugin management
	plugins: {
		list: () => Promise<PluginListResponse>
		get: (pluginId: string) => Promise<PluginInfo>
		install: (request: InstallPluginRequest) => Promise<InstallPluginResponse>
		uninstall: (pluginId: string) => Promise<UninstallPluginResponse>
		enable: (pluginId: string) => Promise<PluginActionResponse>
		disable: (pluginId: string) => Promise<PluginActionResponse>
		reload: (pluginId: string) => Promise<PluginActionResponse>
		update: (pluginId: string, update: PluginConfigUpdate) => Promise<PluginActionResponse>
		execute: (request: PluginExecuteRequest) => Promise<PluginExecuteResponse>
	}

	// System management
	system: {
		info: () => Promise<SystemInfo>
		metrics: () => Promise<SystemMetrics>
		logs: (query: LogQuery) => Promise<LogListResponse>
		command: (request: CommandRequest) => Promise<CommandResponse>
		config: {
			get: () => Promise<ConfigResponse>
			update: (update: ConfigUpdate) => Promise<ConfigResponse>
		}
	}

	// WebSocket
	websocket: {
		connect: () => Promise<{ success: boolean; message?: string }>
		disconnect: () => Promise<{ success: boolean }>
		send: (message: WebSocketMessage) => Promise<{ success: boolean }>
		onMessage: (callback: (message: WebSocketMessage) => void) => void
		offMessage: (callback: (message: WebSocketMessage) => void) => void
		onStatus: (callback: (status: { connected: boolean; error?: string }) => void) => void
	}

	// Application controls
	app: {
		version: () => Promise<string>
		info: () => Promise<AppInfo>
		quit: () => Promise<void>
		minimize: () => Promise<void>
		maximize: () => Promise<void>
		toggleFullscreen: () => Promise<void>
		openExternal: (url: string) => Promise<void>
	}

	// File system
	files: {
		read: (filePath: string) => Promise<{ success: boolean; data?: string }>
		write: (filePath: string, data: string) => Promise<{ success: boolean }>
		selectFile: (options?: any) => Promise<string | null>
		selectDirectory: (options?: any) => Promise<string | null>
		saveFile: (options?: any) => Promise<string | null>
	}

	// Notifications
	notify: {
		show: (options: NotificationOptions) => Promise<void>
		error: (title: string, message: string) => Promise<void>
		success: (title: string, message: string) => Promise<void>
		warning: (title: string, message: string) => Promise<void>
	}

	// Persistent storage
	store: {
		get: <T = any>(key: string, defaultValue?: T) => Promise<T>
		set: (key: string, value: any) => Promise<{ success: boolean }>
		delete: (key: string) => Promise<{ success: boolean }>
		clear: () => Promise<{ success: boolean }>
		keys: () => Promise<string[]>
	}

	// Dialogs
	dialog: {
		showMessageBox: (options: any) => Promise<any>
		showErrorBox: (title: string, content: string) => Promise<void>
		confirm: (title: string, message: string) => Promise<boolean>
	}

	// Event listeners
	on: (channel: string, callback: (...args: any[]) => void) => void
	off: (channel: string, callback: (...args: any[]) => void) => void
	once: (channel: string, callback: (...args: any[]) => void) => void
}

/**
 * Application info
 */
export interface AppInfo {
	name: string
	version: string
	electron: string
	chrome: string
	node: string
	platform: string
}

/**
 * Notification options
 */
export interface NotificationOptions {
	title: string
	body: string
	urgency?: 'normal' | 'critical' | 'low'
}

/**
 * Global window interface extension
 */
declare global {
	interface Window {
		archonAPI: ArchonAPI
	}
}
