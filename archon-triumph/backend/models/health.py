"""
ARCHON TRIUMPH - Health Models
Pydantic models for health check endpoints
"""

from pydantic import BaseModel, Field
from typing import Dict, Any
from datetime import datetime


class HealthResponse(BaseModel):
	"""Health check response"""
	status: str = Field(..., description="Health status (healthy/unhealthy)")
	timestamp: datetime = Field(default_factory=datetime.now)
	service: str = Field(default="ARCHON TRIUMPH Backend")
	version: str = Field(default="1.0.0")


class StatusResponse(BaseModel):
	"""Detailed status response"""
	status: str = Field(..., description="System status")
	uptime_seconds: float = Field(..., description="System uptime in seconds")
	start_time: datetime = Field(..., description="System start time")
	metrics: Dict[str, Any] = Field(default_factory=dict, description="System metrics")
	broker_count: int = Field(default=0, description="Number of connected brokers")
	plugin_count: int = Field(default=0, description="Number of active plugins")
	ws_connections: int = Field(default=0, description="Number of WebSocket connections")
