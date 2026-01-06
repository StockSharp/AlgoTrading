"""
ARCHON TRIUMPH - System Router
System management and operations endpoints
"""

from fastapi import APIRouter, HTTPException
from typing import List
import platform
import psutil
import sys
from datetime import datetime
import time

from models.system import (
	SystemInfo,
	SystemMetrics,
	LogEntry,
	LogListResponse,
	LogQuery,
	CommandRequest,
	CommandResponse,
	ConfigUpdate,
	ConfigResponse,
	ShutdownRequest,
	ShutdownResponse
)
from core.state import app_state
from core.events import event_bus, EventType

router = APIRouter(prefix="/system", tags=["system"])


@router.get("/info", response_model=SystemInfo)
async def get_system_info():
	"""
	Get system information
	"""
	memory = psutil.virtual_memory()

	return SystemInfo(
		platform=platform.system(),
		python_version=sys.version,
		hostname=platform.node(),
		cpu_count=psutil.cpu_count(),
		memory_total=memory.total,
		memory_available=memory.available
	)


@router.get("/metrics", response_model=SystemMetrics)
async def get_system_metrics():
	"""
	Get current system performance metrics
	"""
	memory = psutil.virtual_memory()
	disk = psutil.disk_usage('/')
	network = psutil.net_io_counters()

	return SystemMetrics(
		cpu_percent=psutil.cpu_percent(interval=0.1),
		memory_percent=memory.percent,
		disk_percent=disk.percent,
		network_sent=network.bytes_sent,
		network_recv=network.bytes_recv,
		timestamp=datetime.now()
	)


@router.post("/logs/query", response_model=LogListResponse)
async def query_logs(query: LogQuery):
	"""
	Query system logs with filters
	"""
	# Get events from event bus
	events = event_bus.get_history(limit=query.limit)

	# Convert events to log entries
	logs = [
		LogEntry(
			timestamp=event.timestamp,
			level="INFO",
			message=f"{event.event_type.value}",
			source=event.source,
			metadata=event.data
		)
		for event in events
	]

	# Apply filters
	if query.level:
		logs = [log for log in logs if log.level == query.level]

	if query.source:
		logs = [log for log in logs if log.source == query.source]

	if query.start_time:
		logs = [log for log in logs if log.timestamp >= query.start_time]

	if query.end_time:
		logs = [log for log in logs if log.timestamp <= query.end_time]

	# Apply pagination
	total = len(logs)
	logs = logs[query.offset:query.offset + query.limit]

	return LogListResponse(
		logs=logs,
		total=total,
		filtered=len(logs)
	)


@router.post("/command", response_model=CommandResponse)
async def execute_command(request: CommandRequest):
	"""
	Execute a system command
	"""
	start_time = time.time()

	try:
		# Route command to appropriate handler
		result = await _handle_command(request.command, request.parameters)

		execution_time = time.time() - start_time

		return CommandResponse(
			success=True,
			command=request.command,
			result=result,
			execution_time=execution_time
		)

	except Exception as e:
		execution_time = time.time() - start_time

		# Publish error event
		event_bus.publish(
			EventType.SYSTEM_ERROR,
			{"command": request.command, "error": str(e)},
			source="system"
		)

		return CommandResponse(
			success=False,
			command=request.command,
			error=str(e),
			execution_time=execution_time
		)


async def _handle_command(command: str, parameters: dict):
	"""Handle different system commands"""

	if command == "status":
		return app_state.to_dict()

	elif command == "clear_logs":
		event_bus.clear_history()
		return {"message": "Logs cleared"}

	elif command == "gc":
		import gc
		collected = gc.collect()
		return {"collected_objects": collected}

	elif command == "test":
		return {"message": "Test command executed", "parameters": parameters}

	else:
		raise ValueError(f"Unknown command: {command}")


@router.get("/config", response_model=ConfigResponse)
async def get_config():
	"""
	Get current system configuration
	"""
	# TODO: Implement actual configuration retrieval
	config = {
		"server": {
			"host": "127.0.0.1",
			"port": 8000
		},
		"features": {
			"websocket_enabled": True,
			"auto_restart": True
		}
	}

	return ConfigResponse(
		success=True,
		config=config
	)


@router.put("/config", response_model=ConfigResponse)
async def update_config(update: ConfigUpdate):
	"""
	Update system configuration
	"""
	# TODO: Implement actual configuration update
	# For now, just return the provided config

	return ConfigResponse(
		success=True,
		config=update.config,
		message="Configuration updated successfully"
	)


@router.post("/shutdown", response_model=ShutdownResponse)
async def shutdown_system(request: ShutdownRequest):
	"""
	Shutdown the system
	"""
	shutdown_time = datetime.now()

	if request.delay_seconds > 0:
		shutdown_time = datetime.fromtimestamp(
			shutdown_time.timestamp() + request.delay_seconds
		)

	# Publish shutdown event
	event_bus.publish(
		EventType.SYSTEM_STOPPING,
		{
			"graceful": request.graceful,
			"delay_seconds": request.delay_seconds,
			"shutdown_time": shutdown_time.isoformat()
		},
		source="system"
	)

	app_state.status = app_state.status.__class__.STOPPING

	# TODO: Implement actual graceful shutdown logic
	# For now, just return the response

	return ShutdownResponse(
		success=True,
		message="System shutdown initiated",
		shutdown_time=shutdown_time
	)


@router.post("/restart")
async def restart_system():
	"""
	Restart the backend system
	"""
	# Publish restart event
	event_bus.publish(
		EventType.SYSTEM_STOPPING,
		{"action": "restart"},
		source="system"
	)

	# TODO: Implement actual restart logic

	return {
		"success": True,
		"message": "System restart initiated",
		"timestamp": datetime.now().isoformat()
	}
