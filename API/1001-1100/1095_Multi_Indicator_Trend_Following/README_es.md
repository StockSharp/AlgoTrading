# Estrategia de Seguimiento de Tendencia Multi-Indicador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cruce EMA con confirmación de RSI y volumen. Utiliza stop loss y take profit basados en ATR.

## Detalles

- **Criterios de entrada**: La EMA rápida cruza por encima/debajo de la EMA lenta con filtro RSI y volumen elevado
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop loss y take profit basados en ATR
- **Stops**: Sí, basados en ATR
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `FastMaLength` = 10
  - `SlowMaLength` = 30
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `AtrPeriod` = 14
  - `StopLossAtrMultiplier` = 2
  - `TakeProfitAtrMultiplier` = 3
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, RSI, ATR, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
