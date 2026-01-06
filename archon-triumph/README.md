# ARCHON TRIUMPH

> A powerful Electron-based application with Python backend, featuring real-time communication and modern UI.

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-linux-lightgrey)

## Overview

ARCHON TRIUMPH is a cross-platform desktop application built with:
- **Frontend**: Electron + HTML/CSS/JavaScript
- **Backend**: Python (FastAPI + uvicorn)
- **Communication**: HTTP REST API + WebSocket
- **Architecture**: Modern, modular, and scalable

## Features

### Core Functionality
- ✅ **Real-time Dashboard** - Monitor system status and metrics
- ✅ **Backend Control** - Manage backend server lifecycle
- ✅ **WebSocket Communication** - Bi-directional real-time messaging
- ✅ **Command Execution** - Execute backend commands with parameters
- ✅ **Data Management** - Load, view, and export data
- ✅ **Comprehensive Logging** - Multi-level logging with persistence
- ✅ **Settings Management** - Persistent application configuration

### Technical Features
- **Secure IPC** - Context-isolated inter-process communication
- **Automatic Backend Management** - Backend starts/stops with Electron
- **Health Monitoring** - Continuous health checks and status updates
- **Error Handling** - Comprehensive error handling and recovery
- **Modern UI** - Dark theme with responsive design
- **Development Tools** - Built-in diagnostics and debugging

## Project Structure

```
archon-triumph/
├── backend/                  # Python backend
│   ├── config/              # Configuration files
│   ├── main.py              # Main backend server
│   └── requirements.txt     # Python dependencies
├── electron/                # Electron main process
│   ├── main.js             # Application entry point
│   ├── backend.js          # Backend process manager
│   ├── preload.js          # Preload script (IPC bridge)
│   └── ipc.js              # IPC handlers
├── frontend/               # Frontend UI
│   ├── index.html          # Main HTML
│   ├── css/                # Stylesheets
│   │   ├── main.css        # Main styles
│   │   ├── components.css  # Component styles
│   │   └── animations.css  # Animations
│   └── js/                 # JavaScript modules
│       ├── utils.js        # Utility functions
│       ├── ui.js           # UI management
│       ├── backend.js      # Backend communication
│       ├── websocket.js    # WebSocket handling
│       └── app.js          # Main application
├── scripts/                # Utility scripts
│   ├── dev-start.sh        # Development launcher
│   ├── dev-stop.sh         # Stop development server
│   └── diagnostics.sh      # System diagnostics
├── build/                  # Build scripts
│   └── build.sh            # Production build script
├── logs/                   # Application logs
├── docs/                   # Documentation
├── package.json            # Node.js configuration
├── README.md              # This file
└── STARTUP_CHECKLIST.md   # Setup guide
```

## Quick Start

### Prerequisites

- **Node.js** 16.x or higher
- **npm** 8.x or higher
- **Python** 3.8 or higher
- **pip** (Python package installer)

### Installation

1. **Clone or navigate to the project**:
   ```bash
   cd archon-triumph
   ```

2. **Install Node.js dependencies**:
   ```bash
   npm install
   ```

3. **Install Python dependencies**:
   ```bash
   cd backend
   python3 -m pip install -r requirements.txt
   cd ..
   ```

4. **Make scripts executable**:
   ```bash
   chmod +x scripts/*.sh build/build.sh
   ```

### Running the Application

#### Development Mode

Start the application with development features enabled:

```bash
./scripts/dev-start.sh
```

This will:
- Check and install dependencies
- Start the Python backend server
- Launch the Electron application
- Enable developer tools
- Provide detailed logging

#### Stopping the Application

```bash
./scripts/dev-stop.sh
```

Or press `Ctrl+C` in the terminal where the app is running.

## Usage Guide

### Dashboard Panel

The main dashboard provides:
- **System Status**: Backend online/offline indicator
- **Metrics**: Uptime, connections, and performance data
- **Quick Actions**: Start/stop processing, refresh status

### Control Panel

Advanced controls for:
- **Backend Management**: Restart backend server
- **WebSocket**: Connect/disconnect WebSocket
- **Command Execution**: Execute custom commands with JSON parameters

Example command execution:
```json
{
  "param1": "value1",
  "param2": 123
}
```

### Data Panel

Data management features:
- **Load Data**: Fetch data from backend
- **View Data**: Formatted JSON display
- **Export Data**: Save data to file

### Logs Panel

Real-time logging:
- Color-coded log levels (INFO, WARN, ERROR, SUCCESS)
- Timestamps for all entries
- Clear logs functionality
- Auto-scroll to latest entries

### Settings Panel

Application configuration:
- **Theme**: Dark/Light mode (Dark by default)
- **Auto-connect**: Automatically connect WebSocket on startup
- **Application Info**: Version and system information

## Development

### Architecture

**Electron Main Process** (`electron/main.js`)
- Application lifecycle management
- Window creation and management
- Backend process supervision
- IPC handlers

**Backend Manager** (`electron/backend.js`)
- Python process spawning
- Health monitoring
- Auto-restart capabilities
- Dependency checking

**Preload Script** (`electron/preload.js`)
- Secure API exposure to renderer
- Context isolation
- IPC communication bridge

**Frontend** (`frontend/`)
- Modular JavaScript architecture
- Separation of concerns (UI, backend, WebSocket)
- Utility functions library

**Python Backend** (`backend/main.py`)
- FastAPI REST API
- WebSocket server
- Command processing
- Metrics and monitoring

### API Endpoints

#### REST API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/status` | GET | System status and metrics |
| `/execute` | POST | Execute command |

#### WebSocket

Connect to `/ws` for real-time communication.

**Message Types**:
- `ping/pong` - Connection test
- `command` - Execute command
- `echo` - Echo message back
- `connection` - Connection established
- `error` - Error message

### Adding New Features

1. **Backend Command**: Add handler in `backend/main.py` → `process_command()`
2. **Frontend Function**: Add to appropriate module in `frontend/js/`
3. **UI Element**: Add to `frontend/index.html` and wire up in `app.js`
4. **IPC Handler**: Add to `electron/ipc.js` if needed

## Building for Production

### Build for Linux

```bash
./build/build.sh linux
```

This creates:
- AppImage (portable)
- .deb package (Debian/Ubuntu)

### Build for All Platforms

```bash
./build/build.sh all
```

Build artifacts are created in `dist/` directory.

### Build Configuration

Edit `package.json` → `build` section to customize:
- Application ID
- Product name
- Icons and assets
- Target platforms
- Installer options

## Diagnostics

Run comprehensive system diagnostics:

```bash
./scripts/diagnostics.sh
```

This checks:
- System information
- Dependencies
- Project structure
- Running processes
- Network ports
- Logs
- Disk space
- Performance metrics

A detailed report is saved to `diagnostic-report-TIMESTAMP.txt`.

## Configuration

### Backend Configuration

Edit `backend/config/settings.json`:

```json
{
  "server": {
    "host": "127.0.0.1",
    "port": 8000
  },
  "logging": {
    "level": "INFO"
  },
  "features": {
    "websocket_enabled": true,
    "auto_restart": true
  }
}
```

### Environment Variables

- `ARCHON_HOST` - Backend host (default: 127.0.0.1)
- `ARCHON_PORT` - Backend port (default: 8000)
- `NODE_ENV` - Environment (development/production)

## Logging

Logs are stored in `logs/` directory:

- `backend_YYYYMMDD.log` - Python backend logs
- `electron_YYYYMMDD.log` - Electron process logs
- `renderer_YYYYMMDD.log` - Frontend logs
- `dev_TIMESTAMP.log` - Development session logs

## Troubleshooting

### Common Issues

**Application won't start**
- Check prerequisites: `./scripts/diagnostics.sh`
- Verify dependencies are installed
- Check logs for errors

**Backend fails to start**
- Ensure port 8000 is available
- Check Python version: `python3 --version`
- Verify Python dependencies: `pip list`

**WebSocket connection fails**
- Ensure backend is running
- Check backend status indicator
- Try restarting backend from Control panel

**UI appears broken**
- Clear cache: `rm -rf ~/.config/archon-triumph`
- Reinstall: `npm install`
- Check browser console for errors

For detailed troubleshooting, see `STARTUP_CHECKLIST.md`.

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

MIT License - see LICENSE file for details.

## Support

- **Documentation**: See `docs/` directory
- **Startup Guide**: See `STARTUP_CHECKLIST.md`
- **Diagnostics**: Run `./scripts/diagnostics.sh`
- **Logs**: Check `logs/` directory

## Roadmap

- [ ] Multi-language support
- [ ] Plugin system
- [ ] Advanced data visualization
- [ ] Database integration
- [ ] Cloud sync capabilities
- [ ] Mobile companion app

## Acknowledgments

Built with:
- [Electron](https://www.electronjs.org/)
- [FastAPI](https://fastapi.tiangolo.com/)
- [uvicorn](https://www.uvicorn.org/)
- [WebSocket](https://developer.mozilla.org/en-US/docs/Web/API/WebSocket)

---

**ARCHON TRIUMPH** - Version 1.0.0

Made with ⚡ by the ARCHON TRIUMPH Team
