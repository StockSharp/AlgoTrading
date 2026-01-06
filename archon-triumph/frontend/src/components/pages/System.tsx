/**
 * ARCHON TRIUMPH - System Page
 */

import { useSystemInfo, useSystemMetrics } from '@/hooks/useBackend'

export default function System() {
	const { data: info } = useSystemInfo()
	const { data: metrics } = useSystemMetrics(2000)

	return (
		<div style={{ padding: '24px' }}>
			<h1 style={{ fontSize: '28px', marginBottom: '24px' }}>System</h1>

			<div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '20px' }}>
				<Card title="System Information">
					<Info label="Platform" value={info?.platform || 'N/A'} />
					<Info label="Hostname" value={info?.hostname || 'N/A'} />
					<Info label="CPU Cores" value={info?.cpu_count || 'N/A'} />
					<Info
						label="Memory Total"
						value={info ? `${(info.memory_total / 1024 / 1024 / 1024).toFixed(2)} GB` : 'N/A'}
					/>
				</Card>

				<Card title="Performance Metrics">
					<Info label="CPU Usage" value={metrics ? `${metrics.cpu_percent.toFixed(1)}%` : 'N/A'} />
					<Info label="Memory Usage" value={metrics ? `${metrics.memory_percent.toFixed(1)}%` : 'N/A'} />
					<Info label="Disk Usage" value={metrics ? `${metrics.disk_percent.toFixed(1)}%` : 'N/A'} />
				</Card>
			</div>
		</div>
	)
}

function Card({ title, children }: { title: string; children: React.ReactNode }) {
	return (
		<div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '8px', padding: '20px' }}>
			<h2 style={{ fontSize: '16px', marginBottom: '16px' }}>{title}</h2>
			<div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>{children}</div>
		</div>
	)
}

function Info({ label, value }: { label: string; value: string | number }) {
	return (
		<div style={{ display: 'flex', justifyContent: 'space-between' }}>
			<span style={{ color: 'var(--text-secondary)' }}>{label}:</span>
			<span style={{ color: 'var(--text-primary)', fontWeight: '500' }}>{value}</span>
		</div>
	)
}
