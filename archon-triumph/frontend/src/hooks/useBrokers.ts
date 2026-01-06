/**
 * ARCHON TRIUMPH - Broker Hooks
 * React Query hooks for broker operations
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { useBrokerStore } from '@/stores/useBrokerStore'
import type {
	CreateBrokerRequest,
	UpdateBrokerRequest,
	ConnectBrokerRequest,
	DisconnectBrokerRequest,
} from '@/types/api'

/**
 * Hook to fetch all brokers
 */
export function useBrokers(refetchInterval?: number) {
	const setBrokers = useBrokerStore((state) => state.setBrokers)

	return useQuery({
		queryKey: ['brokers'],
		queryFn: async () => {
			const response = await api.brokers.list()
			setBrokers(response.brokers)
			return response
		},
		refetchInterval,
	})
}

/**
 * Hook to fetch a single broker
 */
export function useBroker(brokerId: string) {
	return useQuery({
		queryKey: ['brokers', brokerId],
		queryFn: () => api.brokers.get(brokerId),
		enabled: !!brokerId,
	})
}

/**
 * Hook to create a new broker
 */
export function useCreateBroker() {
	const queryClient = useQueryClient()
	const addBroker = useBrokerStore((state) => state.addBroker)

	return useMutation({
		mutationFn: (request: CreateBrokerRequest) => api.brokers.create(request),
		onSuccess: async (response) => {
			// Invalidate and refetch brokers list
			await queryClient.invalidateQueries({ queryKey: ['brokers'] })

			// Fetch the new broker details and add to store
			const broker = await api.brokers.get(response.broker_id)
			addBroker(broker)
		},
	})
}

/**
 * Hook to update a broker
 */
export function useUpdateBroker() {
	const queryClient = useQueryClient()
	const updateBroker = useBrokerStore((state) => state.updateBroker)

	return useMutation({
		mutationFn: ({ brokerId, request }: { brokerId: string; request: UpdateBrokerRequest }) =>
			api.brokers.update(brokerId, request),
		onSuccess: async (_, variables) => {
			// Invalidate specific broker and list
			await queryClient.invalidateQueries({ queryKey: ['brokers', variables.brokerId] })
			await queryClient.invalidateQueries({ queryKey: ['brokers'] })

			// Fetch updated broker details
			const broker = await api.brokers.get(variables.brokerId)
			updateBroker(variables.brokerId, broker)
		},
	})
}

/**
 * Hook to delete a broker
 */
export function useDeleteBroker() {
	const queryClient = useQueryClient()
	const removeBroker = useBrokerStore((state) => state.removeBroker)

	return useMutation({
		mutationFn: (brokerId: string) => api.brokers.delete(brokerId),
		onSuccess: (_, brokerId) => {
			// Remove from cache and store
			queryClient.removeQueries({ queryKey: ['brokers', brokerId] })
			queryClient.invalidateQueries({ queryKey: ['brokers'] })
			removeBroker(brokerId)
		},
	})
}

/**
 * Hook to connect a broker
 */
export function useConnectBroker() {
	const queryClient = useQueryClient()
	const updateBroker = useBrokerStore((state) => state.updateBroker)

	return useMutation({
		mutationFn: (request: ConnectBrokerRequest) => api.brokers.connect(request),
		onSuccess: async (response) => {
			// Invalidate broker data
			await queryClient.invalidateQueries({ queryKey: ['brokers', response.broker_id] })
			await queryClient.invalidateQueries({ queryKey: ['brokers'] })

			// Update store with new status
			updateBroker(response.broker_id, { status: response.status })
		},
	})
}

/**
 * Hook to disconnect a broker
 */
export function useDisconnectBroker() {
	const queryClient = useQueryClient()
	const updateBroker = useBrokerStore((state) => state.updateBroker)

	return useMutation({
		mutationFn: (request: DisconnectBrokerRequest) => api.brokers.disconnect(request),
		onSuccess: async (response) => {
			// Invalidate broker data
			await queryClient.invalidateQueries({ queryKey: ['brokers', response.broker_id] })
			await queryClient.invalidateQueries({ queryKey: ['brokers'] })

			// Update store with disconnected status
			updateBroker(response.broker_id, { status: 'disconnected' })
		},
	})
}
