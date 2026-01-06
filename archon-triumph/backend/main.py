from fastapi import FastAPI
from routers import health, brokers, plugins, system

app = FastAPI(title="ARCHON TRIUMPH Backend")

app.include_router(health.router)
app.include_router(brokers.router)
app.include_router(plugins.router)
app.include_router(system.router)

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="127.0.0.1", port=8000, reload=True)
