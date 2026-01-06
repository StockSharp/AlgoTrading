/**
 * ARCHON TRIUMPH - Main App Component
 */

import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/Layout'
import Dashboard from './components/pages/Dashboard'
import Brokers from './components/pages/Brokers'
import Plugins from './components/pages/Plugins'
import System from './components/pages/System'

export default function App() {
	return (
		<BrowserRouter>
			<Layout>
				<Routes>
					<Route path="/" element={<Navigate to="/dashboard" replace />} />
					<Route path="/dashboard" element={<Dashboard />} />
					<Route path="/brokers" element={<Brokers />} />
					<Route path="/plugins" element={<Plugins />} />
					<Route path="/system" element={<System />} />
				</Routes>
			</Layout>
		</BrowserRouter>
	)
}
