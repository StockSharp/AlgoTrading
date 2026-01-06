# backend/models/system.py
from datetime import datetime
from typing import Dict, Any
from pydantic import BaseModel

class SystemInfo(BaseModel):
  version: str
  environment: str
  startedAt: datetime
  hostname: str
  extra: Dict[str, Any] = {}
