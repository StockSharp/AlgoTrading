# backend/models/plugins.py
from typing import Literal, Optional
from pydantic import BaseModel

class PluginStatus(BaseModel):
    id: str
    name: str
    type: Literal["strategy", "risk", "analytics", "execution", "monitoring"]
    status: Literal["loaded", "unloaded", "running", "stopped", "error"]
    version: str
    lastEvent: Optional[str] = None
