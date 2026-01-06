"""
ARCHON TRIUMPH - Plugins Router
Plugin management endpoints
"""

from fastapi import APIRouter, HTTPException
from typing import List
from datetime import datetime
import uuid

from models.plugins import (
	PluginInfo,
	PluginListResponse,
	PluginActionRequest,
	PluginActionResponse,
	InstallPluginRequest,
	InstallPluginResponse,
	UninstallPluginResponse,
	PluginExecuteRequest,
	PluginExecuteResponse,
	PluginConfigUpdate
)
from core.state import app_state, PluginState
from core.events import event_bus, EventType

router = APIRouter(prefix="/plugins", tags=["plugins"])


@router.get("/", response_model=PluginListResponse)
async def list_plugins():
	"""
	Get list of all installed plugins
	"""
	plugins = [
		PluginInfo(
			plugin_id=plugin.plugin_id,
			name=plugin.name,
			version=plugin.version,
			description=plugin.metadata.get("description"),
			author=plugin.metadata.get("author"),
			enabled=plugin.enabled,
			loaded_at=plugin.loaded_at,
			metadata=plugin.metadata
		)
		for plugin in app_state.plugins.values()
	]

	enabled_count = sum(1 for p in plugins if p.enabled)

	return PluginListResponse(
		plugins=plugins,
		total=len(plugins),
		enabled_count=enabled_count
	)


@router.get("/{plugin_id}", response_model=PluginInfo)
async def get_plugin(plugin_id: str):
	"""
	Get specific plugin by ID
	"""
	plugin = app_state.get_plugin(plugin_id)

	if not plugin:
		raise HTTPException(status_code=404, detail=f"Plugin {plugin_id} not found")

	return PluginInfo(
		plugin_id=plugin.plugin_id,
		name=plugin.name,
		version=plugin.version,
		description=plugin.metadata.get("description"),
		author=plugin.metadata.get("author"),
		enabled=plugin.enabled,
		loaded_at=plugin.loaded_at,
		metadata=plugin.metadata
	)


@router.post("/install", response_model=InstallPluginResponse)
async def install_plugin(request: InstallPluginRequest):
	"""
	Install a new plugin
	"""
	plugin_id = str(uuid.uuid4())

	# TODO: Implement actual plugin installation logic
	# For now, create a placeholder plugin

	plugin = PluginState(
		plugin_id=plugin_id,
		name=f"Plugin from {request.plugin_path}",
		version="1.0.0",
		enabled=request.auto_enable,
		loaded_at=datetime.now() if request.auto_enable else None,
		metadata={
			"plugin_path": request.plugin_path,
			"installed_at": datetime.now().isoformat()
		}
	)

	app_state.add_plugin(plugin)

	# Publish event
	event_bus.publish(
		EventType.PLUGIN_LOADED,
		{"plugin_id": plugin_id, "name": plugin.name},
		source="plugins"
	)

	return InstallPluginResponse(
		success=True,
		plugin_id=plugin_id,
		message=f"Plugin installed successfully"
	)


@router.delete("/{plugin_id}", response_model=UninstallPluginResponse)
async def uninstall_plugin(plugin_id: str):
	"""
	Uninstall a plugin
	"""
	plugin = app_state.get_plugin(plugin_id)

	if not plugin:
		raise HTTPException(status_code=404, detail=f"Plugin {plugin_id} not found")

	plugin_name = plugin.name
	app_state.remove_plugin(plugin_id)

	# Publish event
	event_bus.publish(
		EventType.PLUGIN_UNLOADED,
		{"plugin_id": plugin_id, "name": plugin_name},
		source="plugins"
	)

	return UninstallPluginResponse(
		success=True,
		plugin_id=plugin_id,
		message=f"Plugin '{plugin_name}' uninstalled successfully"
	)


@router.post("/{plugin_id}/enable", response_model=PluginActionResponse)
async def enable_plugin(plugin_id: str):
	"""
	Enable a plugin
	"""
	plugin = app_state.get_plugin(plugin_id)

	if not plugin:
		raise HTTPException(status_code=404, detail=f"Plugin {plugin_id} not found")

	plugin.enabled = True
	plugin.loaded_at = datetime.now()
	app_state.add_plugin(plugin)

	# Publish event
	event_bus.publish(
		EventType.PLUGIN_LOADED,
		{"plugin_id": plugin_id, "name": plugin.name},
		source="plugins"
	)

	return PluginActionResponse(
		success=True,
		plugin_id=plugin_id,
		action="enable",
		message=f"Plugin '{plugin.name}' enabled"
	)


@router.post("/{plugin_id}/disable", response_model=PluginActionResponse)
async def disable_plugin(plugin_id: str):
	"""
	Disable a plugin
	"""
	plugin = app_state.get_plugin(plugin_id)

	if not plugin:
		raise HTTPException(status_code=404, detail=f"Plugin {plugin_id} not found")

	plugin.enabled = False
	plugin.loaded_at = None
	app_state.add_plugin(plugin)

	# Publish event
	event_bus.publish(
		EventType.PLUGIN_UNLOADED,
		{"plugin_id": plugin_id, "name": plugin.name},
		source="plugins"
	)

	return PluginActionResponse(
		success=True,
		plugin_id=plugin_id,
		action="disable",
		message=f"Plugin '{plugin.name}' disabled"
	)


@router.post("/{plugin_id}/reload", response_model=PluginActionResponse)
async def reload_plugin(plugin_id: str):
	"""
	Reload a plugin
	"""
	plugin = app_state.get_plugin(plugin_id)

	if not plugin:
		raise HTTPException(status_code=404, detail=f"Plugin {plugin_id} not found")

	# TODO: Implement actual plugin reload logic
	plugin.loaded_at = datetime.now()
	app_state.add_plugin(plugin)

	return PluginActionResponse(
		success=True,
		plugin_id=plugin_id,
		action="reload",
		message=f"Plugin '{plugin.name}' reloaded"
	)


@router.put("/{plugin_id}", response_model=PluginActionResponse)
async def update_plugin_config(plugin_id: str, update: PluginConfigUpdate):
	"""
	Update plugin configuration
	"""
	plugin = app_state.get_plugin(plugin_id)

	if not plugin:
		raise HTTPException(status_code=404, detail=f"Plugin {plugin_id} not found")

	if update.enabled is not None:
		plugin.enabled = update.enabled
		if update.enabled:
			plugin.loaded_at = datetime.now()
		else:
			plugin.loaded_at = None

	if update.metadata is not None:
		plugin.metadata.update(update.metadata)

	app_state.add_plugin(plugin)

	return PluginActionResponse(
		success=True,
		plugin_id=plugin_id,
		action="update",
		message=f"Plugin '{plugin.name}' configuration updated"
	)


@router.post("/execute", response_model=PluginExecuteResponse)
async def execute_plugin_function(request: PluginExecuteRequest):
	"""
	Execute a plugin function
	"""
	plugin = app_state.get_plugin(request.plugin_id)

	if not plugin:
		raise HTTPException(status_code=404, detail=f"Plugin {request.plugin_id} not found")

	if not plugin.enabled:
		raise HTTPException(status_code=400, detail=f"Plugin '{plugin.name}' is not enabled")

	try:
		# TODO: Implement actual plugin function execution
		# For now, return a mock result
		result = {
			"executed": True,
			"function": request.function,
			"params": request.params,
			"timestamp": datetime.now().isoformat()
		}

		return PluginExecuteResponse(
			success=True,
			plugin_id=request.plugin_id,
			function=request.function,
			result=result
		)

	except Exception as e:
		# Publish error event
		event_bus.publish(
			EventType.PLUGIN_ERROR,
			{"plugin_id": request.plugin_id, "error": str(e)},
			source="plugins"
		)

		return PluginExecuteResponse(
			success=False,
			plugin_id=request.plugin_id,
			function=request.function,
			error=str(e)
		)
