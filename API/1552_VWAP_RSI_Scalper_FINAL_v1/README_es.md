# Estrategia de Scalper VWAP RSI FINAL v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de scalping que combina VWAP y RSI con salidas basadas en ATR y límites de operaciones diarias.

## Detalles

- **Criterios de entrada**: Precio relativo al VWAP y EMA con umbrales de RSI dentro de la sesión.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop y objetivo basados en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RsiLength` = 3
  - `RsiOversold` = 35m
  - `RsiOverbought` = 70m
  - `EmaLength` = 50
  - `SessionStart` = 09:00
  - `SessionEnd` = 16:00
  - `MaxTradesPerDay` = 3
  - `AtrLength` = 14
  - `StopAtrMult` = 1m
  - `TargetAtrMult` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Scalping
  - Dirección: Ambos
  - Indicadores: VWAP, RSI, EMA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
