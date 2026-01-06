"""
ARCHON TRIUMPH - Brokers Router
Broker management endpoints
"""

from fastapi import APIRouter, HTTPException
from typing import List
from datetime import datetime
import uuid

from models.brokers import (
	BrokerInfo,
	BrokerListResponse,
	ConnectBrokerRequest,
	ConnectBrokerResponse,
	DisconnectBrokerRequest,
	DisconnectBrokerResponse,
	CreateBrokerRequest,
	CreateBrokerResponse,
	UpdateBrokerRequest,
	UpdateBrokerResponse,
	DeleteBrokerResponse,
	BrokerStatus
)
from core.state import app_state, BrokerState, BrokerStatus as CoreBrokerStatus
from core.events import event_bus, EventType

router = APIRouter(prefix="/brokers", tags=["brokers"])


@router.get("/", response_model=BrokerListResponse)
async def list_brokers():
	"""
	Get list of all configured brokers
	"""
	brokers = [
		BrokerInfo(
			broker_id=broker.broker_id,
			name=broker.name,
			broker_type=broker.metadata.get("broker_type", "custom"),
			status=BrokerStatus(broker.status.value),
			connected_at=broker.connected_at,
			last_error=broker.last_error,
			enabled=broker.metadata.get("enabled", True),
			metadata=broker.metadata
		)
		for broker in app_state.brokers.values()
	]

	return BrokerListResponse(
		brokers=brokers,
		total=len(brokers)
	)


@router.get("/{broker_id}", response_model=BrokerInfo)
async def get_broker(broker_id: str):
	"""
	Get specific broker by ID
	"""
	broker = app_state.get_broker(broker_id)

	if not broker:
		raise HTTPException(status_code=404, detail=f"Broker {broker_id} not found")

	return BrokerInfo(
		broker_id=broker.broker_id,
		name=broker.name,
		broker_type=broker.metadata.get("broker_type", "custom"),
		status=BrokerStatus(broker.status.value),
		connected_at=broker.connected_at,
		last_error=broker.last_error,
		enabled=broker.metadata.get("enabled", True),
		metadata=broker.metadata
	)


@router.post("/", response_model=CreateBrokerResponse)
async def create_broker(request: CreateBrokerRequest):
	"""
	Create a new broker configuration
	"""
	broker_id = str(uuid.uuid4())

	# Create broker state
	broker = BrokerState(
		broker_id=broker_id,
		name=request.name,
		status=CoreBrokerStatus.DISCONNECTED,
		metadata={
			"broker_type": request.broker_type.value,
			"enabled": request.enabled,
			"auto_connect": request.auto_connect,
			"credentials": request.credentials.dict(),
			**request.metadata
		}
	)

	app_state.add_broker(broker)

	# Publish event
	event_bus.publish(
		EventType.BROKER_CONNECTED,
		{"broker_id": broker_id, "name": request.name},
		source="brokers"
	)

	return CreateBrokerResponse(
		success=True,
		broker_id=broker_id,
		message=f"Broker '{request.name}' created successfully"
	)


@router.put("/{broker_id}", response_model=UpdateBrokerResponse)
async def update_broker(broker_id: str, request: UpdateBrokerRequest):
	"""
	Update broker configuration
	"""
	broker = app_state.get_broker(broker_id)

	if not broker:
		raise HTTPException(status_code=404, detail=f"Broker {broker_id} not found")

	# Update fields
	if request.name is not None:
		broker.name = request.name

	if request.credentials is not None:
		broker.metadata["credentials"] = request.credentials.dict()

	if request.enabled is not None:
		broker.metadata["enabled"] = request.enabled

	if request.auto_connect is not None:
		broker.metadata["auto_connect"] = request.auto_connect

	if request.metadata is not None:
		broker.metadata.update(request.metadata)

	app_state.add_broker(broker)  # Update in state

	return UpdateBrokerResponse(
		success=True,
		broker_id=broker_id,
		message=f"Broker '{broker.name}' updated successfully"
	)


@router.delete("/{broker_id}", response_model=DeleteBrokerResponse)
async def delete_broker(broker_id: str):
	"""
	Delete broker configuration
	"""
	broker = app_state.get_broker(broker_id)

	if not broker:
		raise HTTPException(status_code=404, detail=f"Broker {broker_id} not found")

	broker_name = broker.name
	app_state.remove_broker(broker_id)

	# Publish event
	event_bus.publish(
		EventType.BROKER_DISCONNECTED,
		{"broker_id": broker_id, "name": broker_name},
		source="brokers"
	)

	return DeleteBrokerResponse(
		success=True,
		broker_id=broker_id,
		message=f"Broker '{broker_name}' deleted successfully"
	)


@router.post("/connect", response_model=ConnectBrokerResponse)
async def connect_broker(request: ConnectBrokerRequest):
	"""
	Connect to a broker
	"""
	broker = app_state.get_broker(request.broker_id)

	if not broker:
		raise HTTPException(status_code=404, detail=f"Broker {request.broker_id} not found")

	# Simulate connection (in real implementation, this would actually connect)
	broker.status = CoreBrokerStatus.CONNECTING

	try:
		# TODO: Implement actual broker connection logic here
		# For now, just mark as connected
		broker.status = CoreBrokerStatus.CONNECTED
		broker.connected_at = datetime.now()
		broker.last_error = None

		app_state.add_broker(broker)

		# Publish event
		event_bus.publish(
			EventType.BROKER_CONNECTED,
			{"broker_id": request.broker_id, "name": broker.name},
			source="brokers"
		)

		return ConnectBrokerResponse(
			success=True,
			broker_id=request.broker_id,
			status=BrokerStatus.CONNECTED,
			message=f"Connected to broker '{broker.name}'"
		)

	except Exception as e:
		broker.status = CoreBrokerStatus.ERROR
		broker.last_error = str(e)
		app_state.add_broker(broker)

		# Publish error event
		event_bus.publish(
			EventType.BROKER_ERROR,
			{"broker_id": request.broker_id, "error": str(e)},
			source="brokers"
		)

		raise HTTPException(status_code=500, detail=f"Failed to connect: {str(e)}")


@router.post("/disconnect", response_model=DisconnectBrokerResponse)
async def disconnect_broker(request: DisconnectBrokerRequest):
	"""
	Disconnect from a broker
	"""
	broker = app_state.get_broker(request.broker_id)

	if not broker:
		raise HTTPException(status_code=404, detail=f"Broker {request.broker_id} not found")

	# TODO: Implement actual broker disconnection logic here
	broker.status = CoreBrokerStatus.DISCONNECTED
	broker.connected_at = None
	app_state.add_broker(broker)

	# Publish event
	event_bus.publish(
		EventType.BROKER_DISCONNECTED,
		{"broker_id": request.broker_id, "name": broker.name},
		source="brokers"
	)

	return DisconnectBrokerResponse(
		success=True,
		broker_id=request.broker_id,
		message=f"Disconnected from broker '{broker.name}'"
	)
