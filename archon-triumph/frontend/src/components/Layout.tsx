/**
 * ARCHON TRIUMPH - Layout Component
 */

import { ReactNode } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useBackendStatus } from '@/hooks/useBackend'

interface LayoutProps {
	children: ReactNode
}

export default function Layout({ children }: LayoutProps) {
	const location = useLocation()
	const { data: status } = useBackendStatus(5000)

	const navItems = [
		{ path: '/dashboard', label: 'Dashboard', icon: '📊' },
		{ path: '/brokers', label: 'Brokers', icon: '🔌' },
		{ path: '/plugins', label: 'Plugins', icon: '🧩' },
		{ path: '/system', label: 'System', icon: '⚙️' },
	]

	return (
		<div style={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
			{/* Sidebar */}
			<aside
				style={{
					width: '200px',
					background: 'var(--bg-secondary)',
					borderRight: '1px solid var(--border)',
					display: 'flex',
					flexDirection: 'column',
				}}
			>
				<div style={{ padding: '20px', borderBottom: '1px solid var(--border)' }}>
					<h1 style={{ fontSize: '16px', color: 'var(--accent)', fontWeight: 'bold' }}>
						ARCHON TRIUMPH
					</h1>
					<div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '4px' }}>
						{status?.status === 'running' ? '● Online' : '○ Offline'}
					</div>
				</div>

				<nav style={{ flex: 1, padding: '16px', display: 'flex', flexDirection: 'column', gap: '8px' }}>
					{navItems.map((item) => (
						<Link
							key={item.path}
							to={item.path}
							style={{
								padding: '12px 16px',
								background:
									location.pathname === item.path ? 'var(--bg-tertiary)' : 'transparent',
								color:
									location.pathname === item.path ? 'var(--accent)' : 'var(--text-secondary)',
								borderRadius: '6px',
								textDecoration: 'none',
								display: 'flex',
								alignItems: 'center',
								gap: '12px',
								transition: 'all 0.2s',
								borderLeft:
									location.pathname === item.path ? '3px solid var(--accent)' : '3px solid transparent',
							}}
						>
							<span style={{ fontSize: '18px' }}>{item.icon}</span>
							<span>{item.label}</span>
						</Link>
					))}
				</nav>
			</aside>

			{/* Main Content */}
			<main style={{ flex: 1, overflow: 'auto', background: 'var(--bg-primary)' }}>
				{children}
			</main>
		</div>
	)
}
