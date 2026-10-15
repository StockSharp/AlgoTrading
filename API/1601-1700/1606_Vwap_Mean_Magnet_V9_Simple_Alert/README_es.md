# Estrategia VWAP Mean Magnet v9 (Alerta Simple)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta versión simplificada de la estrategia VWAP Mean Magnet usa VWAP y RSI sin filtro de volumen. Las operaciones se abren cuando el precio se desvía del VWAP y el RSI alcanza niveles extremos. Las posiciones se cierran cuando el precio vuelve al VWAP.

## Detalles

- **Criterios de entrada**:
  - **Largo**: precio < VWAP y RSI < sobrevendido.
  - **Corto**: precio > VWAP y RSI > sobrecomprado.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cierre de posición cuando el precio regresa al VWAP.
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `VWAP length` = 60
  - `RSI length` = 14
  - `RSI overbought` = 65
  - `RSI oversold` = 25
  - `Stop loss %` = 0.5
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Intradía
