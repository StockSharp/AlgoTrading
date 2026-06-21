# Estrategia Vegas SuperTrend Enhanced presentTrading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina un canal Vegas con un SuperTrend ajustado.
Entra cuando el SuperTrend cambia de dirección con un multiplicador basado en volatilidad.

## Detalles

- **Criterios de entrada**: cambios de tendencia detectados por SuperTrend ajustado
- **Largo/Corto**: Ambos (configurable)
- **Criterios de salida**: cambio de tendencia opuesto
- **Stops**: No
- **Valores predeterminados**:
  - `AtrPeriod` = 10
  - `VegasWindow` = 100
  - `SuperTrendMultiplier` = 5
  - `VolatilityAdjustment` = 5
  - `TradeDirection` = "Both"
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, SMA, StandardDeviation
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
