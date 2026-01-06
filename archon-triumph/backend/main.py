#!/usr/bin/env python3
"""
ARCHON TRIUMPH - Backend Server
Main entry point for the Python backend service
"""

import os
import sys
import json
import logging
import asyncio
from datetime import datetime
from pathlib import Path
from typing import Dict, Any, Optional

from fastapi import FastAPI, WebSocket, WebSocketDisconnect, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
import uvicorn
from pydantic import BaseModel

# Configure logging
LOG_DIR = Path(__file__).parent.parent / "logs"
LOG_DIR.mkdir(exist_ok=True)

logging.basicConfig(
	level=logging.INFO,
	format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
	handlers=[
		logging.FileHandler(LOG_DIR / f"backend_{datetime.now().strftime('%Y%m%d')}.log"),
		logging.StreamHandler(sys.stdout)
	]
)

logger = logging.getLogger("ARCHON_TRIUMPH")

# Initialize FastAPI application
app = FastAPI(
	title="ARCHON TRIUMPH Backend",
	description="Core backend service for ARCHON TRIUMPH application",
	version="1.0.0"
)

# CORS configuration for Electron frontend
app.add_middleware(
	CORSMiddleware,
	allow_origins=["*"],
	allow_credentials=True,
	allow_methods=["*"],
	allow_headers=["*"],
)

# Application state
class AppState:
	"""Global application state"""
	def __init__(self):
		self.connected_clients: Dict[str, WebSocket] = {}
		self.is_running: bool = False
		self.start_time: datetime = datetime.now()
		self.metrics: Dict[str, Any] = {
			"total_requests": 0,
			"active_connections": 0,
			"errors": 0
		}

state = AppState()

# Pydantic models
class StatusResponse(BaseModel):
	status: str
	uptime_seconds: float
	connected_clients: int
	metrics: Dict[str, Any]

class CommandRequest(BaseModel):
	command: str
	parameters: Optional[Dict[str, Any]] = None

class CommandResponse(BaseModel):
	success: bool
	message: str
	data: Optional[Dict[str, Any]] = None

# Health check endpoint
@app.get("/health")
async def health_check():
	"""Health check endpoint for monitoring"""
	logger.info("Health check requested")
	return JSONResponse({
		"status": "healthy",
		"timestamp": datetime.now().isoformat(),
		"service": "ARCHON TRIUMPH Backend"
	})

# Status endpoint
@app.get("/status", response_model=StatusResponse)
async def get_status():
	"""Get current backend status and metrics"""
	uptime = (datetime.now() - state.start_time).total_seconds()
	state.metrics["total_requests"] += 1

	return StatusResponse(
		status="running" if state.is_running else "idle",
		uptime_seconds=uptime,
		connected_clients=len(state.connected_clients),
		metrics=state.metrics
	)

# Command execution endpoint
@app.post("/execute", response_model=CommandResponse)
async def execute_command(request: CommandRequest):
	"""Execute a command with optional parameters"""
	logger.info(f"Executing command: {request.command}")
	state.metrics["total_requests"] += 1

	try:
		# Command routing logic
		result = await process_command(request.command, request.parameters or {})
		return CommandResponse(
			success=True,
			message=f"Command '{request.command}' executed successfully",
			data=result
		)
	except Exception as e:
		logger.error(f"Command execution failed: {e}")
		state.metrics["errors"] += 1
		raise HTTPException(status_code=500, detail=str(e))

async def process_command(command: str, parameters: Dict[str, Any]) -> Dict[str, Any]:
	"""Process individual commands"""
	commands = {
		"start": start_processing,
		"stop": stop_processing,
		"get_data": get_data,
		"configure": configure_system,
	}

	handler = commands.get(command)
	if not handler:
		raise ValueError(f"Unknown command: {command}")

	return await handler(parameters)

async def start_processing(params: Dict[str, Any]) -> Dict[str, Any]:
	"""Start main processing"""
	state.is_running = True
	logger.info("Processing started")
	return {"status": "started", "timestamp": datetime.now().isoformat()}

async def stop_processing(params: Dict[str, Any]) -> Dict[str, Any]:
	"""Stop main processing"""
	state.is_running = False
	logger.info("Processing stopped")
	return {"status": "stopped", "timestamp": datetime.now().isoformat()}

async def get_data(params: Dict[str, Any]) -> Dict[str, Any]:
	"""Retrieve data based on parameters"""
	data_type = params.get("type", "default")
	logger.info(f"Retrieving data: {data_type}")

	# Placeholder data retrieval logic
	return {
		"type": data_type,
		"data": [],
		"timestamp": datetime.now().isoformat()
	}

async def configure_system(params: Dict[str, Any]) -> Dict[str, Any]:
	"""Configure system parameters"""
	logger.info(f"Configuring system with params: {params}")
	# Configuration logic here
	return {"configured": True, "parameters": params}

# WebSocket endpoint for real-time communication
@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
	"""WebSocket connection handler for real-time communication"""
	client_id = f"client_{len(state.connected_clients) + 1}"
	await websocket.accept()
	state.connected_clients[client_id] = websocket
	state.metrics["active_connections"] = len(state.connected_clients)

	logger.info(f"WebSocket client connected: {client_id}")

	try:
		# Send welcome message
		await websocket.send_json({
			"type": "connection",
			"client_id": client_id,
			"message": "Connected to ARCHON TRIUMPH backend"
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
		if client_id in state.connected_clients:
			del state.connected_clients[client_id]
		state.metrics["active_connections"] = len(state.connected_clients)

async def handle_websocket_message(client_id: str, message: Dict[str, Any]) -> Dict[str, Any]:
	"""Handle incoming WebSocket messages"""
	msg_type = message.get("type", "unknown")

	if msg_type == "ping":
		return {"type": "pong", "timestamp": datetime.now().isoformat()}

	elif msg_type == "command":
		command = message.get("command")
		params = message.get("parameters", {})
		result = await process_command(command, params)
		return {"type": "command_response", "data": result}

	else:
		return {
			"type": "echo",
			"original": message,
			"timestamp": datetime.now().isoformat()
		}

# Broadcast message to all connected clients
async def broadcast_message(message: Dict[str, Any]):
	"""Broadcast message to all connected WebSocket clients"""
	disconnected_clients = []

	for client_id, websocket in state.connected_clients.items():
		try:
			await websocket.send_json(message)
		except Exception as e:
			logger.error(f"Failed to send to {client_id}: {e}")
			disconnected_clients.append(client_id)

	# Clean up disconnected clients
	for client_id in disconnected_clients:
		del state.connected_clients[client_id]

	state.metrics["active_connections"] = len(state.connected_clients)

# Startup event
@app.on_event("startup")
async def startup_event():
	"""Run on application startup"""
	logger.info("="*60)
	logger.info("ARCHON TRIUMPH Backend Starting")
	logger.info("="*60)
	state.start_time = datetime.now()
	logger.info(f"Start time: {state.start_time.isoformat()}")
	logger.info(f"Log directory: {LOG_DIR}")

# Shutdown event
@app.on_event("shutdown")
async def shutdown_event():
	"""Run on application shutdown"""
	logger.info("="*60)
	logger.info("ARCHON TRIUMPH Backend Shutting Down")
	logger.info("="*60)

	# Close all WebSocket connections
	for client_id in list(state.connected_clients.keys()):
		try:
			await state.connected_clients[client_id].close()
		except:
			pass

	logger.info("Shutdown complete")

def main():
	"""Main entry point"""
	# Load configuration
	host = os.getenv("ARCHON_HOST", "127.0.0.1")
	port = int(os.getenv("ARCHON_PORT", "8000"))

	logger.info(f"Starting server on {host}:{port}")

	# Run server
	uvicorn.run(
		app,
		host=host,
		port=port,
		log_level="info",
		access_log=True
	)

if __name__ == "__main__":
	main()
