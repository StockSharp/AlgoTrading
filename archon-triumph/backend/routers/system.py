# backend/routers/system.py
from fastapi import APIRouter
from ..core.state import get_state
from ..models.system import SystemInfo

router = APIRouter(prefix="/system", tags=["system"])

@router.get("/info", response_model=SystemInfo)
async def system_info():
    s = get_state()
    return SystemInfo(
        version=s.version,
        environment=s.environment,
        startedAt=s.started_at,
        hostname=s.hostname,
        extra={},
    )
