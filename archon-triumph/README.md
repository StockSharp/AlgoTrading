# ARCHON TRIUMPH

> **Production-grade trading platform with modular architecture**

## Architecture

### Backend (Python + FastAPI)
- **Modular routers**: health, brokers, plugins, system
- **Pydantic models**: Full type safety
- **Core state management**: Centralized application state
- **Event bus**: Pub/sub messaging
- **WebSocket**: Real-time communication

### Frontend (React + Vite + TypeScript)
- **TypeScript contracts**: Type-safe IPC and API
- **Zustand stores**: Client-side state management
- **React Query**: Data fetching with caching
- **React Router**: Client-side routing
- **Modern UI**: Clean, responsive design

### Electron
- **Context isolation**: Secure IPC bridge
- **Backend manager**: Auto-start/stop Python server
- **Type-safe preload**: Full TypeScript support

## Quick Start

### Prerequisites
- Node.js 18+
- Python 3.8+
- npm/yarn

### Installation

```bash
# Install backend dependencies
cd backend
python3 -m venv ../venv
source ../venv/bin/activate  # On Linux/Mac
pip install -r requirements.txt

# Install frontend dependencies
cd ../frontend
npm install

# Install Electron dependencies
cd ../electron
npm install
```

### Development

```bash
# Terminal 1: Start backend
cd backend
source ../venv/bin/activate
python main.py

# Terminal 2: Start frontend dev server
cd frontend
npm run dev

# Terminal 3: Start Electron
cd electron
npm run dev
```

## Project Structure

```
archon-triumph/
├── backend/
│   ├── main.py                 # FastAPI app entry point
│   ├── routers/               # API route handlers
│   │   ├── health.py          # Health & status endpoints
│   │   ├── brokers.py         # Broker management
│   │   ├── plugins.py         # Plugin system
│   │   └── system.py          # System operations
│   ├── models/                # Pydantic models
│   ├── core/                  # Core functionality
│   │   ├── state.py           # Application state
│   │   └── events.py          # Event bus
│   └── requirements.txt
│
├── frontend/
│   ├── src/
│   │   ├── types/             # TypeScript types
│   │   │   ├── api.ts         # API type definitions
│   │   │   └── ipc.ts         # IPC contracts
│   │   ├── api/
│   │   │   └── client.ts      # Type-safe API client
│   │   ├── stores/            # Zustand stores
│   │   ├── hooks/             # React Query hooks
│   │   ├── components/        # React components
│   │   └── main.tsx           # Entry point
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
│
├── electron/
│   ├── main/
│   │   ├── main.js            # Electron main process
│   │   ├── backend.js         # Backend manager
│   │   ├── preload.js         # IPC bridge
│   │   └── ipc.js             # IPC handlers
│   └── package.json
│
└── scripts/                   # Dev & build scripts
```

## API Endpoints

### Health
- `GET /health` - Health check
- `GET /health/status` - Detailed status

### Brokers
- `GET /brokers` - List all brokers
- `POST /brokers` - Create broker
- `PUT /brokers/{id}` - Update broker
- `DELETE /brokers/{id}` - Delete broker
- `POST /brokers/connect` - Connect to broker
- `POST /brokers/disconnect` - Disconnect from broker

### Plugins
- `GET /plugins` - List all plugins
- `POST /plugins/install` - Install plugin
- `DELETE /plugins/{id}` - Uninstall plugin
- `POST /plugins/{id}/enable` - Enable plugin
- `POST /plugins/{id}/disable` - Disable plugin

### System
- `GET /system/info` - System information
- `GET /system/metrics` - Performance metrics
- `POST /system/command` - Execute command
- `GET /system/config` - Get configuration

### WebSocket
- `WS /ws` - Real-time communication

## Development

### Backend Development
```bash
cd backend
source ../venv/bin/activate
python main.py  # Auto-reload enabled
```

### Frontend Development
```bash
cd frontend
npm run dev  # Vite dev server with HMR
```

### Type Safety
- **Backend**: Pydantic models ensure runtime validation
- **Frontend**: TypeScript provides compile-time safety
- **IPC Bridge**: Typed contracts between main and renderer

## Building

### Frontend Build
```bash
cd frontend
npm run build  # Outputs to frontend/dist
```

### Electron Build
```bash
cd electron
npm run build  # Creates distributable
```

## Key Features

### Modular Backend
- Separate routers for each domain
- Shared state management
- Event-driven architecture
- WebSocket support

### Type-Safe Frontend
- Full TypeScript coverage
- Zustand for state
- React Query for data fetching
- Type-safe IPC bridge

### Clean Architecture
- Separation of concerns
- Testable components
- Scalable structure
- Production-ready

## Next Steps

1. **Implement Electron files**: Complete main.js, backend.js, preload.js, ipc.js
2. **Add dev scripts**: Unified dev launcher
3. **Testing**: Add unit and integration tests
4. **Documentation**: API docs, component docs
5. **CI/CD**: GitHub Actions for builds

## License

MIT
