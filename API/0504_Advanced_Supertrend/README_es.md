# Estrategia Avanzada de Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Avanzada de Supertrend mejora el indicador clásico Supertrend con filtros opcionales de RSI, media móvil y fuerza de tendencia. Entra largo cuando Supertrend cambia a alcista y entra corto cuando se vuelve bajista. El stop loss y take profit opcionales se derivan de múltiplos de ATR.

## Detalles

- **Criterios de entrada**:
  - Supertrend cambia de dirección (bajista→alcista para largo, alcista→bajista para corto).
  - Filtros opcionales: RSI dentro de los límites establecidos, precio relativo a una media móvil, fuerza de tendencia y confirmación de ruptura.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal opuesta de Supertrend o niveles opcionales de stop-loss/take-profit.
- **Stops**: Stop loss y take profit opcionales basados en ATR.
- **Valores predeterminados**:
  - `AtrLength` = 6
  - `Multiplier` = 3.0
  - `UseRsiFilter` = false
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseMaFilter` = true
  - `MaLength` = 50
  - `MaType` = Weighted
  - `UseStopLoss` = true
  - `SlMultiplier` = 3.0
  - `UseTakeProfit` = true
  - `TpMultiplier` = 9.0
  - `UseTrendStrength` = false
  - `MinTrendBars` = 2
  - `UseBreakoutConfirmation` = true
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: Supertrend, RSI, Media Móvil
  - Stops: Basado en ATR
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
