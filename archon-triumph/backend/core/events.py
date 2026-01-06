"""
ARCHON TRIUMPH - Event System
Pub/Sub event bus for application-wide events
"""

from typing import Callable, Dict, List, Any
from datetime import datetime
from dataclasses import dataclass
from enum import Enum


class EventType(str, Enum):
	"""Event types"""
	# System events
	SYSTEM_STARTED = "system.started"
	SYSTEM_STOPPING = "system.stopping"
	SYSTEM_ERROR = "system.error"

	# Broker events
	BROKER_CONNECTED = "broker.connected"
	BROKER_DISCONNECTED = "broker.disconnected"
	BROKER_ERROR = "broker.error"

	# Plugin events
	PLUGIN_LOADED = "plugin.loaded"
	PLUGIN_UNLOADED = "plugin.unloaded"
	PLUGIN_ERROR = "plugin.error"

	# WebSocket events
	WS_CLIENT_CONNECTED = "ws.client.connected"
	WS_CLIENT_DISCONNECTED = "ws.client.disconnected"

	# Data events
	DATA_RECEIVED = "data.received"
	DATA_PROCESSED = "data.processed"


@dataclass
class Event:
	"""Event data structure"""
	event_type: EventType
	timestamp: datetime
	data: Dict[str, Any]
	source: str = "system"


EventHandler = Callable[[Event], None]


class EventBus:
	"""
	Simple event bus for pub/sub messaging
	Allows components to communicate without tight coupling
	"""

	def __init__(self):
		self._handlers: Dict[EventType, List[EventHandler]] = {}
		self._event_history: List[Event] = []
		self._max_history = 1000  # Keep last 1000 events

	def subscribe(self, event_type: EventType, handler: EventHandler):
		"""Subscribe to an event type"""
		if event_type not in self._handlers:
			self._handlers[event_type] = []

		self._handlers[event_type].append(handler)

	def unsubscribe(self, event_type: EventType, handler: EventHandler):
		"""Unsubscribe from an event type"""
		if event_type in self._handlers:
			self._handlers[event_type].remove(handler)

	def publish(self, event_type: EventType, data: Dict[str, Any], source: str = "system"):
		"""Publish an event"""
		event = Event(
			event_type=event_type,
			timestamp=datetime.now(),
			data=data,
			source=source
		)

		# Store in history
		self._event_history.append(event)
		if len(self._event_history) > self._max_history:
			self._event_history.pop(0)

		# Notify handlers
		if event_type in self._handlers:
			for handler in self._handlers[event_type]:
				try:
					handler(event)
				except Exception as e:
					print(f"Error in event handler: {e}")

	def get_history(self, event_type: EventType = None, limit: int = 100) -> List[Event]:
		"""Get event history, optionally filtered by type"""
		history = self._event_history

		if event_type:
			history = [e for e in history if e.event_type == event_type]

		return history[-limit:]

	def clear_history(self):
		"""Clear event history"""
		self._event_history.clear()


# Global event bus instance
event_bus = EventBus()
