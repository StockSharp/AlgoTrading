/**
 * ARCHON TRIUMPH - Brokers Page
 */

import { useBrokers, useConnectBroker, useDisconnectBroker } from '@/hooks/useBrokers'

export default function Brokers() {
	const { data, isLoading } = useBrokers(10000)
	const connect = useConnectBroker()
	const disconnect = useDisconnectBroker()

	if (isLoading) return <div style={{ padding: '24px' }}>Loading...</div>

	return (
		<div style={{ padding: '24px' }}>
			<h1 style={{ fontSize: '28px', marginBottom: '24px' }}>Brokers</h1>

			<div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
				{data?.brokers.map((broker) => (
					<div
						key={broker.broker_id}
						style={{
							background: 'var(--bg-secondary)',
							border: '1px solid var(--border)',
							borderRadius: '8px',
							padding: '20px',
							display: 'flex',
							justifyContent: 'space-between',
							alignItems: 'center',
						}}
					>
						<div>
							<h3 style={{ fontSize: '16px', marginBottom: '8px' }}>{broker.name}</h3>
							<div style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
								Type: {broker.broker_type} | Status: {broker.status}
							</div>
						</div>

						<button
							onClick={() =>
								broker.status === 'connected'
									? disconnect.mutate({ broker_id: broker.broker_id })
									: connect.mutate({ broker_id: broker.broker_id })
							}
							style={{
								padding: '8px 16px',
								background: broker.status === 'connected' ? 'var(--error)' : 'var(--success)',
								color: 'white',
								border: 'none',
								borderRadius: '6px',
							}}
						>
							{broker.status === 'connected' ? 'Disconnect' : 'Connect'}
						</button>
					</div>
				))}

				{!data?.brokers.length && (
					<div style={{ textAlign: 'center', padding: '40px', color: 'var(--text-muted)' }}>
						No brokers configured
					</div>
				)}
			</div>
		</div>
	)
}
