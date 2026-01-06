"""
ARCHON TRIUMPH - Backend Main Application
FastAPI backend with modular router architecture
"""

import sys
from pathlib import Path

# Add backend directory to Python path
backend_dir = Path(__file__).parent
sys.path.insert(0, str(backend_dir))

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager
import uvicorn
import logging
from datetime import datetime
import json

from routers import health, brokers, plugins, system
from core.state import app_state, SystemStatus
from core.events import event_bus, EventType

# Configure logging
logging.basicConfig(
	level=logging.INFO,
	format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger("ARCHON_TRIUMPH")


@asynccontextmanager
async def lifespan(app: FastAPI):
	"""
	Application lifespan manager
	Handles startup and shutdown events
	"""
	# Startup
	logger.info("=" * 60)
	logger.info("ARCHON TRIUMPH Backend Starting")
	logger.info("=" * 60)

	app_state.status = SystemStatus.RUNNING
	app_state.start_time = datetime.now()

	# Publish startup event
	event_bus.publish(
		EventType.SYSTEM_STARTED,
		{"start_time": app_state.start_time.isoformat()},
		source="system"
	)

	logger.info(f"Start time: {app_state.start_time.isoformat()}")
	logger.info("Application state initialized")
	logger.info("Event bus ready")

	yield

	# Shutdown
	logger.info("=" * 60)
	logger.info("ARCHON TRIUMPH Backend Shutting Down")
	logger.info("=" * 60)

	app_state.status = SystemStatus.STOPPING

	# Publish shutdown event
	event_bus.publish(
		EventType.SYSTEM_STOPPING,
		{"uptime_seconds": app_state.get_uptime()},
		source="system"
	)

	# Close all WebSocket connections
	for client_id in list(app_state.ws_connections.keys()):
		try:
			ws = app_state.ws_connections[client_id]
			await ws.close()
		except:
			pass

	logger.info("Shutdown complete")


# Initialize FastAPI application
app = FastAPI(
	title="ARCHON TRIUMPH Backend",
	description="Modular backend for ARCHON TRIUMPH trading platform",
	version="1.0.0",
	lifespan=lifespan
)

# CORS middleware for Electron frontend
app.add_middleware(
	CORSMiddleware,
	allow_origins=["*"],  # In production, specify exact origins
	allow_credentials=True,
	allow_methods=["*"],
	allow_headers=["*"],
)

# Include routers
app.include_router(health.router)
app.include_router(brokers.router)
app.include_router(plugins.router)
app.include_router(system.router)


# WebSocket endpoint for real-time communication
@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
	"""WebSocket connection handler for real-time communication"""
	client_id = f"client_{len(app_state.ws_connections) + 1}"
	await websocket.accept()

	app_state.ws_connections[client_id] = websocket
	app_state.metrics["active_connections"] = len(app_state.ws_connections)

	logger.info(f"WebSocket client connected: {client_id}")

	# Publish connection event
	event_bus.publish(
		EventType.WS_CLIENT_CONNECTED,
		{"client_id": client_id},
		source="websocket"
	)

	try:
		# Send welcome message
		await websocket.send_json({
			"type": "connection",
			"client_id": client_id,
			"message": "Connected to ARCHON TRIUMPH backend",
			"timestamp": datetime.now().isoformat()
		})

		# Message handling loop
		while True:
			data = await websocket.receive_text()
			logger.info(f"Received from {client_id}: {data}")

			try:
				message = json.loads(data)
				response = await handle_websocket_message(client_id, message)
				await websocket.send_json(response)
			except json.JSONDecodeError:
				await websocket.send_json({
					"type": "error",
					"message": "Invalid JSON format"
				})

	except WebSocketDisconnect:
		logger.info(f"WebSocket client disconnected: {client_id}")
	except Exception as e:
		logger.error(f"WebSocket error for {client_id}: {e}")
	finally:
		if client_id in app_state.ws_connections:
			del app_state.ws_connections[client_id]
		app_state.metrics["active_connections"] = len(app_state.ws_connections)

		# Publish disconnection event
		event_bus.publish(
			EventType.WS_CLIENT_DISCONNECTED,
			{"client_id": client_id},
			source="websocket"
		)


async def handle_websocket_message(client_id: str, message: dict):
	"""Handle incoming WebSocket messages"""
	msg_type = message.get("type", "unknown")

	if msg_type == "ping":
		return {
			"type": "pong",
			"timestamp": datetime.now().isoformat()
		}

	elif msg_type == "subscribe":
		# Handle event subscription
		return {
			"type": "subscribed",
			"client_id": client_id,
			"timestamp": datetime.now().isoformat()
		}

	elif msg_type == "command":
		# Handle command execution via WebSocket
		command = message.get("command")
		params = message.get("parameters", {})

		try:
			# Import here to avoid circular dependency
			from routers.system import _handle_command
			result = await _handle_command(command, params)

			return {
				"type": "command_response",
				"success": True,
				"data": result
			}
		except Exception as e:
			return {
				"type": "command_response",
				"success": False,
				"error": str(e)
			}

	else:
		return {
			"type": "echo",
			"original": message,
			"timestamp": datetime.now().isoformat()
		}


# Broadcast function for sending to all connected clients
async def broadcast_message(message: dict):
	"""Broadcast message to all connected WebSocket clients"""
	disconnected_clients = []

	for client_id, websocket in app_state.ws_connections.items():
		try:
			await websocket.send_json(message)
		except Exception as e:
			logger.error(f"Failed to send to {client_id}: {e}")
			disconnected_clients.append(client_id)

	# Clean up disconnected clients
	for client_id in disconnected_clients:
		del app_state.ws_connections[client_id]

	app_state.metrics["active_connections"] = len(app_state.ws_connections)


# Root endpoint
@app.get("/")
async def root():
	"""Root endpoint - API information"""
	return {
		"name": "ARCHON TRIUMPH Backend",
		"version": "1.0.0",
		"status": app_state.status.value,
		"uptime_seconds": app_state.get_uptime(),
		"endpoints": {
			"health": "/health",
			"status": "/health/status",
			"brokers": "/brokers",
			"plugins": "/plugins",
			"system": "/system",
			"websocket": "/ws",
			"docs": "/docs"
		}
	}


def main():
	"""Main entry point"""
	import os

	host = os.getenv("ARCHON_HOST", "127.0.0.1")
	port = int(os.getenv("ARCHON_PORT", "8000"))

	logger.info(f"Starting server on {host}:{port}")

	uvicorn.run(
		"main:app",
		host=host,
		port=port,
		log_level="info",
		reload=True  # Enable auto-reload in development
	)


if __name__ == "__main__":
	main()
