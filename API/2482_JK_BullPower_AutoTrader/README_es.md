# JK BullP AutoTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El JK BullP AutoTrader es un puerto del Expert Advisor original de MetaTrader que se basa en el oscilador Bulls Power. Interpreta la relación entre dos valores consecutivos de Bulls Power para detectar cuándo la fuerza alcista se está desvaneciendo por encima de la línea cero o cuando cae por debajo de cero y se revierte. Las operaciones largas y cortas están protegidas con stops fijos y un trailing stop incremental que se ajusta a medida que la operación se vuelve rentable.

## Detalles

- **Criterios de entrada**: Vender cuando Bulls Power de hace dos barras está por encima de la barra anterior y la barra anterior está por encima de cero. Comprar cuando la barra anterior de Bulls Power está por debajo de cero.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Take profit fijo, stop loss fijo, o trailing stop alcanzado. Las señales opuestas revierten la posición.
- **Stops**: Take profit fijo, stop loss fijo, trailing stop.
- **Valores predeterminados**:
  - `BullsPeriod` = 13
  - `TakeProfitPoints` = 350
  - `StopLossPoints` = 100
  - `TrailingStopPoints` = 100
  - `TrailingStepPoints` = 40
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Bulls Power
  - Stops: Fijo + Trailing
  - Complejidad: Básico
  - Marco temporal: Intradía / Swing (1H)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
