"""
ARCHON TRIUMPH - Core Application State
Central state management for the backend
"""

from datetime import datetime
from typing import Dict, Any, Optional
from dataclasses import dataclass, field
from enum import Enum


class SystemStatus(str, Enum):
	"""System status enumeration"""
	INITIALIZING = "initializing"
	RUNNING = "running"
	IDLE = "idle"
	ERROR = "error"
	STOPPING = "stopping"


class BrokerStatus(str, Enum):
	"""Broker connection status"""
	DISCONNECTED = "disconnected"
	CONNECTING = "connecting"
	CONNECTED = "connected"
	ERROR = "error"


@dataclass
class BrokerState:
	"""State for a single broker connection"""
	broker_id: str
	name: str
	status: BrokerStatus
	connected_at: Optional[datetime] = None
	last_error: Optional[str] = None
	metadata: Dict[str, Any] = field(default_factory=dict)


@dataclass
class PluginState:
	"""State for a single plugin"""
	plugin_id: str
	name: str
	enabled: bool
	version: str
	loaded_at: Optional[datetime] = None
	metadata: Dict[str, Any] = field(default_factory=dict)


class ApplicationState:
	"""
	Global application state
	Thread-safe state management for the entire application
	"""

	def __init__(self):
		self.status: SystemStatus = SystemStatus.INITIALIZING
		self.start_time: datetime = datetime.now()

		# Brokers
		self.brokers: Dict[str, BrokerState] = {}

		# Plugins
		self.plugins: Dict[str, PluginState] = {}

		# Metrics
		self.metrics: Dict[str, Any] = {
			"total_requests": 0,
			"active_connections": 0,
			"errors": 0,
			"broker_connections": 0,
			"active_plugins": 0,
		}

		# WebSocket connections
		self.ws_connections: Dict[str, Any] = {}

	def get_uptime(self) -> float:
		"""Get system uptime in seconds"""
		return (datetime.now() - self.start_time).total_seconds()

	def increment_metric(self, metric: str, amount: int = 1):
		"""Increment a metric counter"""
		if metric in self.metrics:
			self.metrics[metric] += amount

	def add_broker(self, broker: BrokerState):
		"""Add or update a broker"""
		self.brokers[broker.broker_id] = broker
		self._update_broker_count()

	def remove_broker(self, broker_id: str):
		"""Remove a broker"""
		if broker_id in self.brokers:
			del self.brokers[broker_id]
			self._update_broker_count()

	def get_broker(self, broker_id: str) -> Optional[BrokerState]:
		"""Get broker by ID"""
		return self.brokers.get(broker_id)

	def add_plugin(self, plugin: PluginState):
		"""Add or update a plugin"""
		self.plugins[plugin.plugin_id] = plugin
		self._update_plugin_count()

	def remove_plugin(self, plugin_id: str):
		"""Remove a plugin"""
		if plugin_id in self.plugins:
			del self.plugins[plugin_id]
			self._update_plugin_count()

	def get_plugin(self, plugin_id: str) -> Optional[PluginState]:
		"""Get plugin by ID"""
		return self.plugins.get(plugin_id)

	def _update_broker_count(self):
		"""Update broker connection count in metrics"""
		connected = sum(1 for b in self.brokers.values() if b.status == BrokerStatus.CONNECTED)
		self.metrics["broker_connections"] = connected

	def _update_plugin_count(self):
		"""Update active plugin count in metrics"""
		active = sum(1 for p in self.plugins.values() if p.enabled)
		self.metrics["active_plugins"] = active

	def to_dict(self) -> Dict[str, Any]:
		"""Convert state to dictionary for serialization"""
		return {
			"status": self.status.value,
			"uptime": self.get_uptime(),
			"start_time": self.start_time.isoformat(),
			"metrics": self.metrics,
			"brokers": {
				broker_id: {
					"broker_id": broker.broker_id,
					"name": broker.name,
					"status": broker.status.value,
					"connected_at": broker.connected_at.isoformat() if broker.connected_at else None,
					"last_error": broker.last_error,
					"metadata": broker.metadata,
				}
				for broker_id, broker in self.brokers.items()
			},
			"plugins": {
				plugin_id: {
					"plugin_id": plugin.plugin_id,
					"name": plugin.name,
					"enabled": plugin.enabled,
					"version": plugin.version,
					"loaded_at": plugin.loaded_at.isoformat() if plugin.loaded_at else None,
					"metadata": plugin.metadata,
				}
				for plugin_id, plugin in self.plugins.items()
			},
		}


# Global application state instance
app_state = ApplicationState()
