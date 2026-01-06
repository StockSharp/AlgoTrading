/**
 * ARCHON TRIUMPH - Plugin Store
 * Plugin state management using Zustand
 */

import { create } from 'zustand'
import type { PluginInfo } from '@/types/api'

interface PluginState {
	// Data
	plugins: PluginInfo[]
	selectedPluginId: string | null

	// Actions
	setPlugins: (plugins: PluginInfo[]) => void
	addPlugin: (plugin: PluginInfo) => void
	updatePlugin: (pluginId: string, updates: Partial<PluginInfo>) => void
	removePlugin: (pluginId: string) => void
	selectPlugin: (pluginId: string | null) => void

	// Computed
	getSelectedPlugin: () => PluginInfo | null
	getEnabledPlugins: () => PluginInfo[]
}

export const usePluginStore = create<PluginState>((set, get) => ({
	// Initial state
	plugins: [],
	selectedPluginId: null,

	// Actions
	setPlugins: (plugins) => set({ plugins }),

	addPlugin: (plugin) =>
		set((state) => ({
			plugins: [...state.plugins, plugin],
		})),

	updatePlugin: (pluginId, updates) =>
		set((state) => ({
			plugins: state.plugins.map((plugin) =>
				plugin.plugin_id === pluginId ? { ...plugin, ...updates } : plugin
			),
		})),

	removePlugin: (pluginId) =>
		set((state) => ({
			plugins: state.plugins.filter((plugin) => plugin.plugin_id !== pluginId),
			selectedPluginId:
				state.selectedPluginId === pluginId ? null : state.selectedPluginId,
		})),

	selectPlugin: (pluginId) => set({ selectedPluginId: pluginId }),

	// Computed
	getSelectedPlugin: () => {
		const { plugins, selectedPluginId } = get()
		return plugins.find((p) => p.plugin_id === selectedPluginId) || null
	},

	getEnabledPlugins: () => {
		const { plugins } = get()
		return plugins.filter((p) => p.enabled)
	},
}))
