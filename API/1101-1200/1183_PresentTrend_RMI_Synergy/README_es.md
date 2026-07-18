# Estrategia PresentTrend RMI Synergy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

PresentTrend RMI Synergy combina un filtro de momentum basado en RSI con un trailing stop ATR estilo SuperTrend. Las entradas ocurren cuando el momentum supera los umbrales y el precio está alineado con la tendencia. El stop sigue dinámicamente el precio utilizando una media móvil y una banda ATR.

Los backtests muestran un rendimiento estable en mercados tendenciales como las criptomonedas.

## Detalles

- **Criterios de entrada**: RMI por encima de 60 con precio sobre la media móvil para largos; RMI por debajo de 40 con precio bajo la media móvil para cortos.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Trailing stop basado en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RmiPeriod` = 21
  - `SuperTrendLength` = 5
  - `SuperTrendMultiplier` = 4.0m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: RSI, ATR, SMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
