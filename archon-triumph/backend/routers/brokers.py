# backend/routers/brokers.py
from fastapi import APIRouter
from ..core.state import get_state
from ..models.brokers import BrokerConnectionStatus

router = APIRouter(prefix="/brokers", tags=["brokers"])

@router.get("", response_model=list[BrokerConnectionStatus])
async def list_brokers():
    return [BrokerConnectionStatus(**b) for b in get_state().brokers]

@router.post("/{broker_id}/connect")
async def connect_broker(broker_id: str):
    # stub: mark broker connected
    state = get_state()
    for b in state.brokers:
        if b["id"] == broker_id:
            b["status"] = "connected"
    return {"ok": True}

@router.post("/{broker_id}/disconnect")
async def disconnect_broker(broker_id: str):
    state = get_state()
    for b in state.brokers:
        if b["id"] == broker_id:
            b["status"] = "disconnected"
    return {"ok": True}
