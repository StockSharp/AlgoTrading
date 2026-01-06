# ARCHON TRIUMPH - Startup Checklist

This checklist ensures your development environment is properly configured to run ARCHON TRIUMPH.

## Prerequisites

### System Requirements

- [ ] **Operating System**: Linux (Ubuntu 20.04+, Debian 10+, or equivalent)
- [ ] **RAM**: Minimum 4GB (8GB recommended)
- [ ] **Disk Space**: At least 2GB free space
- [ ] **Internet Connection**: Required for initial setup

### Required Software

- [ ] **Node.js**: Version 16.x or higher
  ```bash
  node --version  # Should show v16.x.x or higher
  ```

- [ ] **npm**: Version 8.x or higher
  ```bash
  npm --version  # Should show 8.x.x or higher
  ```

- [ ] **Python**: Version 3.8 or higher
  ```bash
  python3 --version  # Should show 3.8.x or higher
  ```

- [ ] **pip**: Python package installer
  ```bash
  python3 -m pip --version
  ```

### Optional Tools

- [ ] **Git**: For version control
- [ ] **curl**: For testing API endpoints
- [ ] **jq**: For JSON parsing in scripts

## Installation Steps

### 1. Install System Dependencies

#### Ubuntu/Debian
```bash
sudo apt update
sudo apt install -y nodejs npm python3 python3-pip build-essential
```

#### Other Linux
Consult your distribution's package manager.

### 2. Navigate to Project Directory
```bash
cd /path/to/archon-triumph
```

### 3. Install Node.js Dependencies
```bash
npm install
```

**Expected output**: Should install without errors and create `node_modules/` directory.

- [ ] `node_modules/` directory exists
- [ ] No installation errors

### 4. Install Python Dependencies
```bash
cd backend
python3 -m pip install -r requirements.txt
cd ..
```

**Expected packages**:
- fastapi
- uvicorn
- websockets
- pydantic

- [ ] All Python packages installed successfully
- [ ] No dependency conflicts

### 5. Verify Project Structure

Run diagnostics to verify everything is set up correctly:
```bash
./scripts/diagnostics.sh
```

- [ ] All directories exist
- [ ] All key files present
- [ ] No missing dependencies

## First Run

### Development Mode

1. **Make scripts executable** (if not already):
   ```bash
   chmod +x scripts/*.sh
   chmod +x build/build.sh
   ```

2. **Start the application**:
   ```bash
   ./scripts/dev-start.sh
   ```

3. **Verify startup**:
   - [ ] Loading screen appears
   - [ ] Backend starts (check logs)
   - [ ] Main window opens
   - [ ] Backend status shows "Online"
   - [ ] No errors in console

4. **Test basic functionality**:
   - [ ] Navigate between panels (Dashboard, Control, Data, Logs, Settings)
   - [ ] Click "Refresh Status" - should update metrics
   - [ ] Check backend status indicator (should be green/online)

5. **Test WebSocket connection**:
   - [ ] Navigate to Control panel
   - [ ] Click "Connect WebSocket"
   - [ ] WebSocket indicator in sidebar turns green
   - [ ] No connection errors in logs

6. **Stop the application**:
   ```bash
   ./scripts/dev-stop.sh
   ```

   Or press `Ctrl+C` in the terminal running the app.

## Common Issues and Solutions

### Issue: "Python not found"
**Solution**: Ensure Python 3 is installed and in your PATH:
```bash
which python3
python3 --version
```

### Issue: "npm install fails"
**Solution**:
1. Clear npm cache: `npm cache clean --force`
2. Delete `node_modules/`: `rm -rf node_modules`
3. Reinstall: `npm install`

### Issue: "Backend fails to start"
**Solution**:
1. Check if port 8000 is already in use:
   ```bash
   netstat -tuln | grep 8000
   # or
   ss -tuln | grep 8000
   ```
2. Kill any process using the port
3. Check Python dependencies are installed

### Issue: "Electron window doesn't open"
**Solution**:
1. Check terminal for error messages
2. Verify all frontend files exist
3. Run diagnostics: `./scripts/diagnostics.sh`
4. Check logs in `logs/` directory

### Issue: "Permission denied" when running scripts
**Solution**: Make scripts executable:
```bash
chmod +x scripts/*.sh build/build.sh
```

### Issue: "Backend port already in use"
**Solution**:
1. Stop any running instances:
   ```bash
   ./scripts/dev-stop.sh
   ```
2. Or manually kill processes:
   ```bash
   pkill -f "python.*main.py"
   ```

## Verification Checklist

After successful startup, verify these features:

### Dashboard Panel
- [ ] System status displays correctly
- [ ] Uptime counter updates
- [ ] Connection count shows
- [ ] Start/Stop/Refresh buttons work

### Control Panel
- [ ] Backend restart works
- [ ] WebSocket connect/disconnect works
- [ ] Command execution works
- [ ] Results display correctly

### Data Panel
- [ ] Load data button works
- [ ] Data displays in the panel
- [ ] Export data function works

### Logs Panel
- [ ] Log entries appear
- [ ] Timestamps are correct
- [ ] Log levels color-coded
- [ ] Clear logs button works

### Settings Panel
- [ ] Application info displays
- [ ] Theme selection works (if implemented)
- [ ] Settings persist on restart

## Post-Installation Steps

### Configure Auto-Start (Optional)
To have ARCHON TRIUMPH start on system boot, create a systemd service or desktop autostart entry.

### Review Logs
Check the logs directory for any warnings or errors:
```bash
ls -lh logs/
```

### Update Configuration
Edit `backend/config/settings.json` to customize:
- Server host and port
- Logging levels
- Feature flags

## Performance Tuning

### For Low-Memory Systems
- Limit log file sizes
- Adjust backend worker count
- Disable detailed logging

### For Production
- Set `NODE_ENV=production`
- Enable auto-restart
- Configure log rotation
- Set up monitoring

## Next Steps

1. **Explore the application**: Familiarize yourself with all panels and features
2. **Read the documentation**: See `README.md` for detailed feature descriptions
3. **Run diagnostics regularly**: Use `./scripts/diagnostics.sh` to monitor health
4. **Check logs**: Review logs for any issues or warnings
5. **Build for production**: Use `./build/build.sh` when ready to deploy

## Support

If you encounter issues not covered in this checklist:

1. Run diagnostics: `./scripts/diagnostics.sh`
2. Check logs in `logs/` directory
3. Review error messages carefully
4. Ensure all prerequisites are met
5. Try a clean reinstall:
   ```bash
   rm -rf node_modules backend/__pycache__
   npm install
   cd backend && python3 -m pip install -r requirements.txt
   ```

## Checklist Complete

Once you've verified all items above, ARCHON TRIUMPH is ready for use!

- [ ] All prerequisites installed
- [ ] All dependencies installed
- [ ] Application starts successfully
- [ ] All panels functional
- [ ] No errors in logs
- [ ] Startup checklist complete ✓

---

**Version**: 1.0.0
**Last Updated**: 2026-01-06
