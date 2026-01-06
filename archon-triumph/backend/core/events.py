# backend/core/events.py
from datetime import datetime
from typing import Literal, Optional, Dict, Any
from pydantic import BaseModel

ArchonEventSource = Literal["backend", "broker", "plugin", "strategy", "system"]
ArchonEventLevel = Literal["info", "warn", "error", "debug"]

class ArchonEvent(BaseModel):
    id: str
    timestamp: datetime
    type: str
    source: ArchonEventSource
    level: ArchonEventLevel
    message: str
    metadata: Optional[Dict[str, Any]] = None
