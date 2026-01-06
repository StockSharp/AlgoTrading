# backend/models/brokers.py
from typing import Literal, Optional
from datetime import datetime
from pydantic import BaseModel

class BrokerConnectionStatus(BaseModel):
    id: str
    name: str
    type: Literal["mt4", "mt5", "oanda", "stocksharp", "custom"]
    status: Literal["connected", "disconnected", "connecting", "error"]
    latencyMs: Optional[float] = None
    lastHeartbeat: Optional[datetime] = None
    errorMessage: Optional[str] = None
