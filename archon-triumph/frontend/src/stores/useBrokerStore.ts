/**
 * ARCHON TRIUMPH - Broker Store
 * Broker state management using Zustand
 */

import { create } from 'zustand'
import type { BrokerInfo } from '@/types/api'

interface BrokerState {
	// Data
	brokers: BrokerInfo[]
	selectedBrokerId: string | null

	// Actions
	setBrokers: (brokers: BrokerInfo[]) => void
	addBroker: (broker: BrokerInfo) => void
	updateBroker: (brokerId: string, updates: Partial<BrokerInfo>) => void
	removeBroker: (brokerId: string) => void
	selectBroker: (brokerId: string | null) => void

	// Computed
	getSelectedBroker: () => BrokerInfo | null
	getConnectedBrokers: () => BrokerInfo[]
}

export const useBrokerStore = create<BrokerState>((set, get) => ({
	// Initial state
	brokers: [],
	selectedBrokerId: null,

	// Actions
	setBrokers: (brokers) => set({ brokers }),

	addBroker: (broker) =>
		set((state) => ({
			brokers: [...state.brokers, broker],
		})),

	updateBroker: (brokerId, updates) =>
		set((state) => ({
			brokers: state.brokers.map((broker) =>
				broker.broker_id === brokerId ? { ...broker, ...updates } : broker
			),
		})),

	removeBroker: (brokerId) =>
		set((state) => ({
			brokers: state.brokers.filter((broker) => broker.broker_id !== brokerId),
			selectedBrokerId:
				state.selectedBrokerId === brokerId ? null : state.selectedBrokerId,
		})),

	selectBroker: (brokerId) => set({ selectedBrokerId: brokerId }),

	// Computed
	getSelectedBroker: () => {
		const { brokers, selectedBrokerId } = get()
		return brokers.find((b) => b.broker_id === selectedBrokerId) || null
	},

	getConnectedBrokers: () => {
		const { brokers } = get()
		return brokers.filter((b) => b.status === 'connected')
	},
}))
