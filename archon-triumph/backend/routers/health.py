"""
ARCHON TRIUMPH - Health Router
Health check and status endpoints
"""

from fastapi import APIRouter
from models.health import HealthResponse, StatusResponse
from core.state import app_state
from datetime import datetime

router = APIRouter(prefix="/health", tags=["health"])


@router.get("/", response_model=HealthResponse)
async def health_check():
	"""
	Basic health check endpoint
	Returns simple health status
	"""
	return HealthResponse(
		status="healthy",
		timestamp=datetime.now(),
		service="ARCHON TRIUMPH Backend",
		version="1.0.0"
	)


@router.get("/status", response_model=StatusResponse)
async def get_status():
	"""
	Detailed status endpoint
	Returns comprehensive system status and metrics
	"""
	return StatusResponse(
		status=app_state.status.value,
		uptime_seconds=app_state.get_uptime(),
		start_time=app_state.start_time,
		metrics=app_state.metrics,
		broker_count=len(app_state.brokers),
		plugin_count=len([p for p in app_state.plugins.values() if p.enabled]),
		ws_connections=len(app_state.ws_connections)
	)


@router.get("/ping")
async def ping():
	"""
	Simple ping endpoint for connectivity testing
	"""
	return {"pong": True, "timestamp": datetime.now().isoformat()}
