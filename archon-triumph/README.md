# ARCHON TRIUMPH

> Clean, modular trading platform architecture

## Philosophy

**Predictable. Modular. Replaceable.**

This is not a demo. This is a foundation designed to scale without breaking.

## Architecture

### Backend (`/backend`)
- **FastAPI** with clean routers
- **In-memory state** (stub for now, easy to replace)
- **psutil** for system metrics
- **Pydantic** models for type safety

### Frontend (`/frontend`)
- **React + Vite + SWC** (fastest dev loop)
- **React Router** for navigation
- **React Query** for data fetching
- **Zustand** for client state (ready to add)
- **Inline styles** (until design stabilizes)

## Quick Start

### Backend

```bash
cd backend
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
python main.py
```

Backend runs on `http://127.0.0.1:8000`

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend runs on `http://localhost:3000`

## Structure

```
archon-triumph/
├── backend/
│   ├── main.py              # FastAPI app
│   ├── core/
│   │   ├── state.py         # In-memory state
│   │   └── events.py        # Event system
│   ├── models/              # Pydantic models
│   │   ├── health.py
│   │   ├── brokers.py
│   │   ├── plugins.py
│   │   └── system.py
│   ├── routers/             # API routes
│   │   ├── health.py
│   │   ├── brokers.py
│   │   ├── plugins.py
│   │   └── system.py
│   └── requirements.txt
│
└── frontend/
    ├── src/
    │   ├── main.tsx
    │   ├── App.tsx
    │   ├── layout/
    │   │   └── ShellLayout.tsx
    │   └── modules/
    │       └── dashboard/
    │           └── DashboardPage.tsx
    ├── package.json
    └── vite.config.ts
```

## API Endpoints

- `GET /health` - Backend health status
- `GET /brokers` - List all brokers
- `POST /brokers/{id}/connect` - Connect broker
- `POST /brokers/{id}/disconnect` - Disconnect broker
- `GET /plugins` - List all plugins
- `POST /plugins/{id}/toggle` - Toggle plugin
- `GET /system/info` - System information

## Current Status

✅ **Backend**: Clean, minimal, ready to extend
✅ **Frontend**: Grid layout, stub panels
⏳ **Next**: Zustand stores, React Query hooks, real panels

## Philosophy

1. **Start simple** - Stubs over abstractions
2. **Stay modular** - Easy to replace any layer
3. **Scale gradually** - Add complexity only when needed
4. **Keep it clean** - Code should be obvious

---

Built with precision. Ready to scale.
