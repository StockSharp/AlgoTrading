# Plantilla de Estrategia Basada en R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en RSI con dimensionamiento de posición gestionado por riesgo y tipos de stop configurables.

## Detalles

- **Criterios de entrada**:
  - Largo cuando el RSI cruza por debajo de `OversoldLevel`.
  - Corto cuando el RSI cruza por encima de `OverboughtLevel`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss o take-profit usando el múltiplo `TpRValue`.
- **Stops**:
  - Fixed, Atr, Percentage o Ticks.
- **Valores predeterminados**:
  - `RiskPerTradePercent` = 1
  - `RsiLength` = 14
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `StopLossType` = Fixed
  - `SlValue` = 100
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `TpRValue` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RSI, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Variable
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
