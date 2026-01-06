"""
ARCHON TRIUMPH - Broker Models
Pydantic models for broker management
"""

from pydantic import BaseModel, Field
from typing import Dict, Any, List, Optional
from datetime import datetime
from enum import Enum


class BrokerType(str, Enum):
	"""Broker types"""
	INTERACTIVE_BROKERS = "interactive_brokers"
	ALPACA = "alpaca"
	BINANCE = "binance"
	COINBASE = "coinbase"
	CUSTOM = "custom"


class BrokerStatus(str, Enum):
	"""Broker connection status"""
	DISCONNECTED = "disconnected"
	CONNECTING = "connecting"
	CONNECTED = "connected"
	ERROR = "error"


class BrokerCredentials(BaseModel):
	"""Broker connection credentials"""
	api_key: Optional[str] = Field(None, description="API key")
	api_secret: Optional[str] = Field(None, description="API secret")
	account_id: Optional[str] = Field(None, description="Account ID")
	additional_params: Dict[str, Any] = Field(default_factory=dict)


class BrokerConfig(BaseModel):
	"""Broker configuration"""
	broker_id: str = Field(..., description="Unique broker identifier")
	name: str = Field(..., description="Broker display name")
	broker_type: BrokerType = Field(..., description="Type of broker")
	credentials: BrokerCredentials = Field(..., description="Connection credentials")
	enabled: bool = Field(default=True, description="Whether broker is enabled")
	auto_connect: bool = Field(default=False, description="Auto-connect on startup")
	metadata: Dict[str, Any] = Field(default_factory=dict, description="Additional metadata")


class BrokerInfo(BaseModel):
	"""Broker information and status"""
	broker_id: str
	name: str
	broker_type: BrokerType
	status: BrokerStatus
	connected_at: Optional[datetime] = None
	last_error: Optional[str] = None
	enabled: bool
	metadata: Dict[str, Any] = Field(default_factory=dict)


class BrokerListResponse(BaseModel):
	"""Response for listing brokers"""
	brokers: List[BrokerInfo]
	total: int = Field(..., description="Total number of brokers")


class ConnectBrokerRequest(BaseModel):
	"""Request to connect a broker"""
	broker_id: str = Field(..., description="Broker ID to connect")


class ConnectBrokerResponse(BaseModel):
	"""Response for broker connection"""
	success: bool
	broker_id: str
	status: BrokerStatus
	message: str = Field(default="")


class DisconnectBrokerRequest(BaseModel):
	"""Request to disconnect a broker"""
	broker_id: str = Field(..., description="Broker ID to disconnect")


class DisconnectBrokerResponse(BaseModel):
	"""Response for broker disconnection"""
	success: bool
	broker_id: str
	message: str = Field(default="")


class CreateBrokerRequest(BaseModel):
	"""Request to create a new broker configuration"""
	name: str
	broker_type: BrokerType
	credentials: BrokerCredentials
	enabled: bool = True
	auto_connect: bool = False
	metadata: Dict[str, Any] = Field(default_factory=dict)


class CreateBrokerResponse(BaseModel):
	"""Response for broker creation"""
	success: bool
	broker_id: str
	message: str = Field(default="Broker created successfully")


class UpdateBrokerRequest(BaseModel):
	"""Request to update broker configuration"""
	name: Optional[str] = None
	credentials: Optional[BrokerCredentials] = None
	enabled: Optional[bool] = None
	auto_connect: Optional[bool] = None
	metadata: Optional[Dict[str, Any]] = None


class UpdateBrokerResponse(BaseModel):
	"""Response for broker update"""
	success: bool
	broker_id: str
	message: str = Field(default="Broker updated successfully")


class DeleteBrokerResponse(BaseModel):
	"""Response for broker deletion"""
	success: bool
	broker_id: str
	message: str = Field(default="Broker deleted successfully")
