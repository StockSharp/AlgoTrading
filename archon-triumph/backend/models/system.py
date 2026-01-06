"""
ARCHON TRIUMPH - System Models
Pydantic models for system management
"""

from pydantic import BaseModel, Field
from typing import Dict, Any, List, Optional
from datetime import datetime


class SystemInfo(BaseModel):
	"""System information"""
	platform: str = Field(..., description="Operating system platform")
	python_version: str = Field(..., description="Python version")
	hostname: str = Field(..., description="System hostname")
	cpu_count: int = Field(..., description="Number of CPU cores")
	memory_total: int = Field(..., description="Total memory in bytes")
	memory_available: int = Field(..., description="Available memory in bytes")


class SystemMetrics(BaseModel):
	"""System performance metrics"""
	cpu_percent: float = Field(..., description="CPU usage percentage")
	memory_percent: float = Field(..., description="Memory usage percentage")
	disk_percent: float = Field(..., description="Disk usage percentage")
	network_sent: int = Field(..., description="Network bytes sent")
	network_recv: int = Field(..., description="Network bytes received")
	timestamp: datetime = Field(default_factory=datetime.now)


class LogEntry(BaseModel):
	"""Log entry"""
	timestamp: datetime
	level: str = Field(..., description="Log level (DEBUG, INFO, WARNING, ERROR)")
	message: str = Field(..., description="Log message")
	source: str = Field(default="system", description="Log source")
	metadata: Dict[str, Any] = Field(default_factory=dict)


class LogListResponse(BaseModel):
	"""Response for log listing"""
	logs: List[LogEntry]
	total: int
	filtered: int


class LogQuery(BaseModel):
	"""Query parameters for log retrieval"""
	level: Optional[str] = None
	source: Optional[str] = None
	start_time: Optional[datetime] = None
	end_time: Optional[datetime] = None
	limit: int = Field(default=100, le=1000)
	offset: int = Field(default=0, ge=0)


class CommandRequest(BaseModel):
	"""System command execution request"""
	command: str = Field(..., description="Command to execute")
	parameters: Dict[str, Any] = Field(default_factory=dict, description="Command parameters")


class CommandResponse(BaseModel):
	"""System command execution response"""
	success: bool
	command: str
	result: Any = None
	error: Optional[str] = None
	execution_time: float = Field(..., description="Execution time in seconds")


class ConfigUpdate(BaseModel):
	"""Configuration update request"""
	config: Dict[str, Any] = Field(..., description="Configuration values to update")


class ConfigResponse(BaseModel):
	"""Configuration response"""
	success: bool
	config: Dict[str, Any]
	message: str = Field(default="")


class BackupRequest(BaseModel):
	"""Backup creation request"""
	include_data: bool = Field(default=True)
	include_logs: bool = Field(default=False)


class BackupResponse(BaseModel):
	"""Backup creation response"""
	success: bool
	backup_id: str
	backup_path: str
	size_bytes: int
	created_at: datetime
	message: str = Field(default="Backup created successfully")


class RestoreRequest(BaseModel):
	"""Backup restore request"""
	backup_id: str = Field(..., description="Backup ID to restore")


class RestoreResponse(BaseModel):
	"""Backup restore response"""
	success: bool
	backup_id: str
	message: str = Field(default="Backup restored successfully")


class ShutdownRequest(BaseModel):
	"""Shutdown request"""
	graceful: bool = Field(default=True, description="Graceful shutdown")
	delay_seconds: int = Field(default=0, ge=0, le=60, description="Delay before shutdown")


class ShutdownResponse(BaseModel):
	"""Shutdown response"""
	success: bool
	message: str = Field(default="System shutting down")
	shutdown_time: datetime
