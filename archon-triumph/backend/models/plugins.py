"""
ARCHON TRIUMPH - Plugin Models
Pydantic models for plugin management
"""

from pydantic import BaseModel, Field
from typing import Dict, Any, List, Optional
from datetime import datetime


class PluginInfo(BaseModel):
	"""Plugin information"""
	plugin_id: str = Field(..., description="Unique plugin identifier")
	name: str = Field(..., description="Plugin display name")
	version: str = Field(..., description="Plugin version")
	description: Optional[str] = Field(None, description="Plugin description")
	author: Optional[str] = Field(None, description="Plugin author")
	enabled: bool = Field(default=False, description="Whether plugin is enabled")
	loaded_at: Optional[datetime] = Field(None, description="When plugin was loaded")
	metadata: Dict[str, Any] = Field(default_factory=dict, description="Additional metadata")


class PluginListResponse(BaseModel):
	"""Response for listing plugins"""
	plugins: List[PluginInfo]
	total: int = Field(..., description="Total number of plugins")
	enabled_count: int = Field(..., description="Number of enabled plugins")


class PluginConfigUpdate(BaseModel):
	"""Plugin configuration update"""
	enabled: Optional[bool] = None
	metadata: Optional[Dict[str, Any]] = None


class PluginActionRequest(BaseModel):
	"""Request for plugin action (enable/disable/reload)"""
	plugin_id: str = Field(..., description="Plugin ID")


class PluginActionResponse(BaseModel):
	"""Response for plugin action"""
	success: bool
	plugin_id: str
	action: str = Field(..., description="Action performed")
	message: str = Field(default="")


class InstallPluginRequest(BaseModel):
	"""Request to install a new plugin"""
	plugin_path: str = Field(..., description="Path or URL to plugin")
	auto_enable: bool = Field(default=False, description="Automatically enable after install")


class InstallPluginResponse(BaseModel):
	"""Response for plugin installation"""
	success: bool
	plugin_id: str
	message: str = Field(default="Plugin installed successfully")


class UninstallPluginResponse(BaseModel):
	"""Response for plugin uninstallation"""
	success: bool
	plugin_id: str
	message: str = Field(default="Plugin uninstalled successfully")


class PluginExecuteRequest(BaseModel):
	"""Request to execute plugin function"""
	plugin_id: str
	function: str = Field(..., description="Function name to execute")
	params: Dict[str, Any] = Field(default_factory=dict, description="Function parameters")


class PluginExecuteResponse(BaseModel):
	"""Response for plugin execution"""
	success: bool
	plugin_id: str
	function: str
	result: Any = None
	error: Optional[str] = None
