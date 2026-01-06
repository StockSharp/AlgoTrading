/**
 * ARCHON TRIUMPH - Backend Hooks
 * React Query hooks for backend data fetching
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'

/**
 * Hook to fetch backend health
 */
export function useBackendHealth(refetchInterval = 5000) {
	return useQuery({
		queryKey: ['backend', 'health'],
		queryFn: () => api.backend.health(),
		refetchInterval,
		retry: 3,
	})
}

/**
 * Hook to fetch backend status
 */
export function useBackendStatus(refetchInterval = 5000) {
	return useQuery({
		queryKey: ['backend', 'status'],
		queryFn: () => api.backend.status(),
		refetchInterval,
		retry: 3,
	})
}

/**
 * Hook to fetch system info
 */
export function useSystemInfo() {
	return useQuery({
		queryKey: ['system', 'info'],
		queryFn: () => api.system.info(),
		staleTime: Infinity, // System info doesn't change
	})
}

/**
 * Hook to fetch system metrics
 */
export function useSystemMetrics(refetchInterval = 2000) {
	return useQuery({
		queryKey: ['system', 'metrics'],
		queryFn: () => api.system.metrics(),
		refetchInterval,
	})
}

/**
 * Hook to execute system command
 */
export function useSystemCommand() {
	const queryClient = useQueryClient()

	return useMutation({
		mutationFn: (request: Parameters<typeof api.system.command>[0]) =>
			api.system.command(request),
		onSuccess: () => {
			// Invalidate relevant queries after command execution
			queryClient.invalidateQueries({ queryKey: ['backend'] })
		},
	})
}
