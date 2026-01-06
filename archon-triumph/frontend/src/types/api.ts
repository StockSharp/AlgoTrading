/**
 * ARCHON TRIUMPH - API Type Definitions
 * TypeScript types matching the Python backend models
 */

// ============================================================================
// Health & Status
// ============================================================================

export interface HealthResponse {
	status: string
	timestamp: string
	service: string
	version: string
}

export interface StatusResponse {
	status: string
	uptime_seconds: number
	start_time: string
	metrics: Record<string, number>
	broker_count: number
	plugin_count: number
	ws_connections: number
}

// ============================================================================
// Brokers
// ============================================================================

export enum BrokerType {
	INTERACTIVE_BROKERS = 'interactive_brokers',
	ALPACA = 'alpaca',
	BINANCE = 'binance',
	COINBASE = 'coinbase',
	CUSTOM = 'custom',
}

export enum BrokerStatus {
	DISCONNECTED = 'disconnected',
	CONNECTING = 'connecting',
	CONNECTED = 'connected',
	ERROR = 'error',
}

export interface BrokerCredentials {
	api_key?: string
	api_secret?: string
	account_id?: string
	additional_params?: Record<string, any>
}

export interface BrokerInfo {
	broker_id: string
	name: string
	broker_type: BrokerType
	status: BrokerStatus
	connected_at?: string
	last_error?: string
	enabled: boolean
	metadata: Record<string, any>
}

export interface BrokerListResponse {
	brokers: BrokerInfo[]
	total: number
}

export interface CreateBrokerRequest {
	name: string
	broker_type: BrokerType
	credentials: BrokerCredentials
	enabled?: boolean
	auto_connect?: boolean
	metadata?: Record<string, any>
}

export interface CreateBrokerResponse {
	success: boolean
	broker_id: string
	message?: string
}

export interface UpdateBrokerRequest {
	name?: string
	credentials?: BrokerCredentials
	enabled?: boolean
	auto_connect?: boolean
	metadata?: Record<string, any>
}

export interface UpdateBrokerResponse {
	success: boolean
	broker_id: string
	message?: string
}

export interface ConnectBrokerRequest {
	broker_id: string
}

export interface ConnectBrokerResponse {
	success: boolean
	broker_id: string
	status: BrokerStatus
	message?: string
}

export interface DisconnectBrokerRequest {
	broker_id: string
}

export interface DisconnectBrokerResponse {
	success: boolean
	broker_id: string
	message?: string
}

export interface DeleteBrokerResponse {
	success: boolean
	broker_id: string
	message?: string
}

// ============================================================================
// Plugins
// ============================================================================

export interface PluginInfo {
	plugin_id: string
	name: string
	version: string
	description?: string
	author?: string
	enabled: boolean
	loaded_at?: string
	metadata: Record<string, any>
}

export interface PluginListResponse {
	plugins: PluginInfo[]
	total: number
	enabled_count: number
}

export interface InstallPluginRequest {
	plugin_path: string
	auto_enable?: boolean
}

export interface InstallPluginResponse {
	success: boolean
	plugin_id: string
	message?: string
}

export interface UninstallPluginResponse {
	success: boolean
	plugin_id: string
	message?: string
}

export interface PluginActionResponse {
	success: boolean
	plugin_id: string
	action: string
	message?: string
}

export interface PluginExecuteRequest {
	plugin_id: string
	function: string
	params?: Record<string, any>
}

export interface PluginExecuteResponse {
	success: boolean
	plugin_id: string
	function: string
	result?: any
	error?: string
}

export interface PluginConfigUpdate {
	enabled?: boolean
	metadata?: Record<string, any>
}

// ============================================================================
// System
// ============================================================================

export interface SystemInfo {
	platform: string
	python_version: string
	hostname: string
	cpu_count: number
	memory_total: number
	memory_available: number
}

export interface SystemMetrics {
	cpu_percent: number
	memory_percent: number
	disk_percent: number
	network_sent: number
	network_recv: number
	timestamp: string
}

export interface LogEntry {
	timestamp: string
	level: string
	message: string
	source: string
	metadata: Record<string, any>
}

export interface LogListResponse {
	logs: LogEntry[]
	total: number
	filtered: number
}

export interface LogQuery {
	level?: string
	source?: string
	start_time?: string
	end_time?: string
	limit?: number
	offset?: number
}

export interface CommandRequest {
	command: string
	parameters?: Record<string, any>
}

export interface CommandResponse {
	success: boolean
	command: string
	result?: any
	error?: string
	execution_time: number
}

export interface ConfigResponse {
	success: boolean
	config: Record<string, any>
	message?: string
}

export interface ConfigUpdate {
	config: Record<string, any>
}

// ============================================================================
// WebSocket
// ============================================================================

export interface WebSocketMessage {
	type: string
	[key: string]: any
}

export interface WSConnectionMessage extends WebSocketMessage {
	type: 'connection'
	client_id: string
	message: string
	timestamp: string
}

export interface WSPongMessage extends WebSocketMessage {
	type: 'pong'
	timestamp: string
}

export interface WSCommandResponse extends WebSocketMessage {
	type: 'command_response'
	success: boolean
	data?: any
	error?: string
}

export interface WSErrorMessage extends WebSocketMessage {
	type: 'error'
	message: string
}
