# Estrategia de Bot de Scalping con Ruptura de Sesión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Session Breakout Scalper opera rupturas del rango de precios formado durante una sesión predefinida.

## Detalles

- **Criterios de entrada**: el precio rompe por encima del máximo de la sesión o por debajo del mínimo de la sesión
- **Largo/Corto**: Ambos
- **Criterios de salida**: take profit o stop loss
- **Stops**: ATR o fijo
- **Valores predeterminados**:
  - `SessionStart` = 01:00
  - `SessionEnd` = 02:00
  - `TakeProfit` = 100
  - `StopLoss` = 50
  - `UseAtrStop` = true
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `CandleType` = time frame 1 minute
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: ATR
  - Stops: ATR
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
