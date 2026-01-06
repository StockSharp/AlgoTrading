# backend/core/state.py
from datetime import datetime
from typing import List
from .events import ArchonEvent

class BackendState:
    def __init__(self) -> None:
        self.started_at = datetime.utcnow()
        self.version = "0.1.0"
        self.environment = "DEV"
        self.hostname = "archon-local"
        self.brokers: List[dict] = []
        self.plugins: List[dict] = []
        self.events: List[ArchonEvent] = []

    def add_event(self, event: ArchonEvent) -> None:
        self.events.append(event)
        self.events = self.events[-500:]

state = BackendState()

def get_state() -> BackendState:
    return state
