# backend/routers/plugins.py
from fastapi import APIRouter
from ..core.state import get_state
from ..models.plugins import PluginStatus

router = APIRouter(prefix="/plugins", tags=["plugins"])

@router.get("", response_model=list[PluginStatus])
async def list_plugins():
    return [PluginStatus(**p) for p in get_state().plugins]

@router.post("/{plugin_id}/toggle")
async def toggle_plugin(plugin_id: str):
    state = get_state()
    for p in state.plugins:
        if p["id"] == plugin_id:
            p["status"] = "running" if p["status"] != "running" else "stopped"
    return {"ok": True}
