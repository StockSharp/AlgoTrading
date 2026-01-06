/**
 * ARCHON TRIUMPH - Plugins Page
 */

import { usePlugins, useEnablePlugin, useDisablePlugin } from '@/hooks/usePlugins'

export default function Plugins() {
	const { data, isLoading } = usePlugins(10000)
	const enable = useEnablePlugin()
	const disable = useDisablePlugin()

	if (isLoading) return <div style={{ padding: '24px' }}>Loading...</div>

	return (
		<div style={{ padding: '24px' }}>
			<h1 style={{ fontSize: '28px', marginBottom: '24px' }}>Plugins</h1>

			<div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
				{data?.plugins.map((plugin) => (
					<div
						key={plugin.plugin_id}
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
							<h3 style={{ fontSize: '16px', marginBottom: '8px' }}>{plugin.name}</h3>
							<div style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
								Version: {plugin.version} | {plugin.enabled ? 'Enabled' : 'Disabled'}
							</div>
						</div>

						<button
							onClick={() =>
								plugin.enabled
									? disable.mutate(plugin.plugin_id)
									: enable.mutate(plugin.plugin_id)
							}
							style={{
								padding: '8px 16px',
								background: plugin.enabled ? 'var(--warning)' : 'var(--success)',
								color: 'white',
								border: 'none',
								borderRadius: '6px',
							}}
						>
							{plugin.enabled ? 'Disable' : 'Enable'}
						</button>
					</div>
				))}

				{!data?.plugins.length && (
					<div style={{ textAlign: 'center', padding: '40px', color: 'var(--text-muted)' }}>
						No plugins installed
					</div>
				)}
			</div>
		</div>
	)
}
