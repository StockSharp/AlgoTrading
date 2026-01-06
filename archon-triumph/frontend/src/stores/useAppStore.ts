/**
 * ARCHON TRIUMPH - Application Store
 * Global application state using Zustand
 */

import { create } from 'zustand'
import type { StatusResponse, AppInfo } from '@/types/api'

interface AppState {
	// Backend status
	backendStatus: StatusResponse | null
	isBackendOnline: boolean

	// App info
	appInfo: AppInfo | null

	// WebSocket
	wsConnected: boolean
	wsError: string | null

	// UI state
	sidebarCollapsed: boolean
	theme: 'light' | 'dark'

	// Loading states
	isLoading: boolean
	loadingMessage: string

	// Actions
	setBackendStatus: (status: StatusResponse | null) => void
	setBackendOnline: (online: boolean) => void
	setAppInfo: (info: AppInfo) => void
	setWsConnected: (connected: boolean) => void
	setWsError: (error: string | null) => void
	toggleSidebar: () => void
	setTheme: (theme: 'light' | 'dark') => void
	setLoading: (loading: boolean, message?: string) => void
}

export const useAppStore = create<AppState>((set) => ({
	// Initial state
	backendStatus: null,
	isBackendOnline: false,
	appInfo: null,
	wsConnected: false,
	wsError: null,
	sidebarCollapsed: false,
	theme: 'dark',
	isLoading: false,
	loadingMessage: '',

	// Actions
	setBackendStatus: (status) => set({ backendStatus: status }),

	setBackendOnline: (online) => set({ isBackendOnline: online }),

	setAppInfo: (info) => set({ appInfo: info }),

	setWsConnected: (connected) =>
		set({ wsConnected: connected, wsError: connected ? null : undefined }),

	setWsError: (error) => set({ wsError: error }),

	toggleSidebar: () => set((state) => ({ sidebarCollapsed: !state.sidebarCollapsed })),

	setTheme: (theme) => set({ theme }),

	setLoading: (loading, message = '') => set({ isLoading: loading, loadingMessage: message }),
}))
