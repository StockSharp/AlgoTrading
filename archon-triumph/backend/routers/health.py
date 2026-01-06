# backend/routers/health.py
from fastapi import APIRouter
from datetime import datetime
import psutil
from ..core.state import get_state
from ..models.health import BackendHealthStatus

router = APIRouter(prefix="/health", tags=["health"])

@router.get("", response_model=BackendHealthStatus)
async def get_health():
    state = get_state()
    now = datetime.utcnow()
    uptime = int((now - state.started_at).total_seconds())
    cpu = psutil.cpu_percent(interval=None)
    mem = psutil.virtual_memory().used / (1024 * 1024)
    return BackendHealthStatus(
        status="healthy",
        uptimeSeconds=uptime,
        lastCheck=now,
        version=state.version,
        cpuPercent=cpu,
        memoryMB=mem,
        workers=1,
    )
