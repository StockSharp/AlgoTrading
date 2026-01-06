# backend/models/health.py
from typing import Literal, Optional
from datetime import datetime
from pydantic import BaseModel

BackendStatusState = Literal["starting", "healthy", "degraded", "stopped", "error"]

class BackendHealthStatus(BaseModel):
    status: BackendStatusState
    uptimeSeconds: int
    lastCheck: datetime
    version: str
    cpuPercent: Optional[float] = None
    memoryMB: Optional[float] = None
    workers: Optional[int] = None
    details: Optional[str] = None
