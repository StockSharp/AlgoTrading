/**
 * ARCHON TRIUMPH - Dashboard Page
 */

import { useBackendStatus, useSystemMetrics } from '@/hooks/useBackend'
import { useBrokers } from '@/hooks/useBrokers'
import { usePlugins } from '@/hooks/usePlugins'

export default function Dashboard() {
	const { data: status } = useBackendStatus(5000)
	const { data: metrics } = useSystemMetrics(2000)
	const { data: brokers } = useBrokers()
	const { data: plugins } = usePlugins()

	return (
		<div style={{ padding: '24px' }}>
			<h1 style={{ fontSize: '28px', marginBottom: '24px' }}>Dashboard</h1>

			<div
				style={{
					display: 'grid',
					gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
					gap: '20px',
				}}
			>
				{/* System Status Card */}
				<Card title="System Status">
					<Metric label="Status" value={status?.status || 'Unknown'} />
					<Metric
						label="Uptime"
						value={
							status?.uptime_seconds
								? `${Math.floor(status.uptime_seconds / 3600)}h ${Math.floor((status.uptime_seconds % 3600) / 60)}m`
								: 'N/A'
						}
					/>
					<Metric label="WS Connections" value={status?.ws_connections || 0} />
				</Card>

				{/* Brokers Card */}
				<Card title="Brokers">
					<Metric label="Total" value={brokers?.total || 0} />
					<Metric
						label="Connected"
						value={brokers?.brokers.filter((b) => b.status === 'connected').length || 0}
					/>
				</Card>

				{/* Plugins Card */}
				<Card title="Plugins">
					<Metric label="Total" value={plugins?.total || 0} />
					<Metric label="Enabled" value={plugins?.enabled_count || 0} />
				</Card>

				{/* System Metrics Card */}
				<Card title="System Metrics">
					<Metric
						label="CPU"
						value={metrics?.cpu_percent ? `${metrics.cpu_percent.toFixed(1)}%` : 'N/A'}
					/>
					<Metric
						label="Memory"
						value={
							metrics?.memory_percent ? `${metrics.memory_percent.toFixed(1)}%` : 'N/A'
						}
					/>
					<Metric
						label="Disk"
						value={metrics?.disk_percent ? `${metrics.disk_percent.toFixed(1)}%` : 'N/A'}
					/>
				</Card>
			</div>
		</div>
	)
}

function Card({ title, children }: { title: string; children: React.ReactNode }) {
	return (
		<div
			style={{
				background: 'var(--bg-secondary)',
				border: '1px solid var(--border)',
				borderRadius: '8px',
				padding: '20px',
			}}
		>
			<h2 style={{ fontSize: '16px', marginBottom: '16px', color: 'var(--text-primary)' }}>
				{title}
			</h2>
			<div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>{children}</div>
		</div>
	)
}

function Metric({ label, value }: { label: string; value: string | number }) {
	return (
		<div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
			<span style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>{label}:</span>
			<span style={{ color: 'var(--accent)', fontWeight: '600' }}>{value}</span>
		</div>
	)
}
