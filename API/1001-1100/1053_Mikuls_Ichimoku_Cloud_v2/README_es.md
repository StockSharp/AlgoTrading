# Estrategia Mikul's Ichimoku Cloud v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura usando Ichimoku Cloud con un filtro de media móvil opcional. Las posiciones se gestionan con un trailing stop (ATR, porcentaje o reglas Ichimoku) y take-profit opcional.

## Detalles

- **Criterios de entrada**: Tenkan-sen cruza por encima de Kijun-sen con el precio sobre la nube, o una fuerte ruptura por encima de una nube verde.
- **Largo/Corto**: Solo largo.
- **Criterios de salida**: Trailing stop o reversión Ichimoku, take-profit opcional.
- **Stops**: Trailing.
- **Valores predeterminados**:
  - `TrailSource` = `LowsHighs`
  - `TrailMethod` = `Atr`
  - `TrailPercent` = 10
  - `SwingLookback` = 7
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1
  - `AddIchiExit` = false
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 25
  - `UseMaFilter` = false
  - `MaType` = `Ema`
  - `MaLength` = 200
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouBPeriod` = 52
  - `Displacement` = 26
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: Ichimoku, ATR
  - Stops: Trailing
  - Complejidad: Medio
  - Marco temporal: Intradía (1h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
