# Estrategia de Ruptura de Momentum de Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina niveles de ruptura basados en ATR con filtro de tendencia EMA y momentum RSI para capturar movimientos fuertes.

## Detalles

- **Criterios de entrada**: el precio cierra por encima/debajo de los niveles de ruptura ATR con confirmación de EMA y RSI
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop loss basado en ATR y take profit con ratio riesgo-recompensa 1:2
- **Stops**: ATR
- **Valores predeterminados**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `Lookback` = 20
  - `EmaPeriod` = 50
  - `RsiPeriod` = 14
  - `RsiLongThreshold` = 50
  - `RsiShortThreshold` = 50
  - `RiskReward` = 2
  - `AtrStopMultiplier` = 1
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: ATR, EMA, RSI, Highest, Lowest
  - Stops: ATR
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
