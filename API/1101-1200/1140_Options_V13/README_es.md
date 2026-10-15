# Estrategia de Opciones V1.3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia de cruce de EMA con RSI, stop y take-profit basados en ATR, y filtro de volumen. El sistema puede requerir opcionalmente un rompimiento del rango de apertura y cierra posiciones a las 15:55 hora de Nueva York. Las operaciones están bloqueadas durante sesiones predefinidas y un intervalo de no operación especificado por el usuario.

## Detalles

- **Criterios de entrada**:
  - **Largo**: la EMA corta cruza por encima de la EMA larga, RSI ≥ `RsiLongThreshold`, volumen ≥ SMA del volumen, opcionalmente cierre > máximo del rango de apertura.
  - **Corto**: la EMA corta cruza por debajo de la EMA larga, RSI ≤ `RsiShortThreshold`, volumen ≥ SMA del volumen, opcionalmente cierre < mínimo del rango de apertura.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop-loss y take-profit basados en ATR.
  - Cruce opuesto de EMA.
  - Cierre automático a las 15:55 hora NY.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaShortLength = 8`
  - `EmaLongLength = 28`
  - `RsiLength = 12`
  - `AtrLength = 14`
  - `SlMultiplier = 1.4`
  - `TpSlRatio = 4`
  - `VolumeMaLength = 20`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Configurable
  - Indicadores: EMA, RSI, ATR, SMA
  - Stops: Sí
  - Marco temporal: Intradía
