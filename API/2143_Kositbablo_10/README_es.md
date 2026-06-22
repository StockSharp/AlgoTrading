# Kositbablo 10
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia multi-marco temporal para EURUSD que utiliza señales RSI y EMA.
Verifica indicadores diarios y horarios y abre órdenes de mercado cuando ambos filtros de tendencia coinciden.

## Parámetros
- **Take Profit** – take profit en puntos.
- **Stop Loss** – stop loss en puntos.
- **Turbo Mode** – permitir nuevas operaciones incluso si existe una posición.

## Reglas
- Ir largo cuando el RSI(11) diario < 60, el RSI(5) horario < 48 y EMA20 > EMA2.
- Ir corto cuando el RSI(22) diario > 38, el RSI(20) horario > 60 y EMA23 > EMA12.
- Las operaciones se realizan solo después de que la vela horaria se haya completado.
