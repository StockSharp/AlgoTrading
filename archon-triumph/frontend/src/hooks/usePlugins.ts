/**
 * ARCHON TRIUMPH - Plugin Hooks
 * React Query hooks for plugin operations
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { usePluginStore } from '@/stores/usePluginStore'
import type { InstallPluginRequest, PluginExecuteRequest, PluginConfigUpdate } from '@/types/api'

/**
 * Hook to fetch all plugins
 */
export function usePlugins(refetchInterval?: number) {
	const setPlugins = usePluginStore((state) => state.setPlugins)

	return useQuery({
		queryKey: ['plugins'],
		queryFn: async () => {
			const response = await api.plugins.list()
			setPlugins(response.plugins)
			return response
		},
		refetchInterval,
	})
}

/**
 * Hook to fetch a single plugin
 */
export function usePlugin(pluginId: string) {
	return useQuery({
		queryKey: ['plugins', pluginId],
		queryFn: () => api.plugins.get(pluginId),
		enabled: !!pluginId,
	})
}

/**
 * Hook to install a plugin
 */
export function useInstallPlugin() {
	const queryClient = useQueryClient()
	const addPlugin = usePluginStore((state) => state.addPlugin)

	return useMutation({
		mutationFn: (request: InstallPluginRequest) => api.plugins.install(request),
		onSuccess: async (response) => {
			await queryClient.invalidateQueries({ queryKey: ['plugins'] })

			const plugin = await api.plugins.get(response.plugin_id)
			addPlugin(plugin)
		},
	})
}

/**
 * Hook to uninstall a plugin
 */
export function useUninstallPlugin() {
	const queryClient = useQueryClient()
	const removePlugin = usePluginStore((state) => state.removePlugin)

	return useMutation({
		mutationFn: (pluginId: string) => api.plugins.uninstall(pluginId),
		onSuccess: (_, pluginId) => {
			queryClient.removeQueries({ queryKey: ['plugins', pluginId] })
			queryClient.invalidateQueries({ queryKey: ['plugins'] })
			removePlugin(pluginId)
		},
	})
}

/**
 * Hook to enable a plugin
 */
export function useEnablePlugin() {
	const queryClient = useQueryClient()
	const updatePlugin = usePluginStore((state) => state.updatePlugin)

	return useMutation({
		mutationFn: (pluginId: string) => api.plugins.enable(pluginId),
		onSuccess: async (_, pluginId) => {
			await queryClient.invalidateQueries({ queryKey: ['plugins', pluginId] })
			await queryClient.invalidateQueries({ queryKey: ['plugins'] })

			updatePlugin(pluginId, { enabled: true })
		},
	})
}

/**
 * Hook to disable a plugin
 */
export function useDisablePlugin() {
	const queryClient = useQueryClient()
	const updatePlugin = usePluginStore((state) => state.updatePlugin)

	return useMutation({
		mutationFn: (pluginId: string) => api.plugins.disable(pluginId),
		onSuccess: async (_, pluginId) => {
			await queryClient.invalidateQueries({ queryKey: ['plugins', pluginId] })
			await queryClient.invalidateQueries({ queryKey: ['plugins'] })

			updatePlugin(pluginId, { enabled: false })
		},
	})
}

/**
 * Hook to reload a plugin
 */
export function useReloadPlugin() {
	const queryClient = useQueryClient()

	return useMutation({
		mutationFn: (pluginId: string) => api.plugins.reload(pluginId),
		onSuccess: async (_, pluginId) => {
			await queryClient.invalidateQueries({ queryKey: ['plugins', pluginId] })
			await queryClient.invalidateQueries({ queryKey: ['plugins'] })
		},
	})
}

/**
 * Hook to update plugin configuration
 */
export function useUpdatePlugin() {
	const queryClient = useQueryClient()
	const updatePlugin = usePluginStore((state) => state.updatePlugin)

	return useMutation({
		mutationFn: ({ pluginId, update }: { pluginId: string; update: PluginConfigUpdate }) =>
			api.plugins.update(pluginId, update),
		onSuccess: async (_, variables) => {
			await queryClient.invalidateQueries({ queryKey: ['plugins', variables.pluginId] })
			await queryClient.invalidateQueries({ queryKey: ['plugins'] })

			if (variables.update.enabled !== undefined) {
				updatePlugin(variables.pluginId, { enabled: variables.update.enabled })
			}
		},
	})
}

/**
 * Hook to execute plugin function
 */
export function useExecutePlugin() {
	return useMutation({
		mutationFn: (request: PluginExecuteRequest) => api.plugins.execute(request),
	})
}
