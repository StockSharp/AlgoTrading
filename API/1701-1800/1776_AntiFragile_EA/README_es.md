# Estrategia AntiFragile EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de rejilla que coloca órdenes limitadas en capas por encima y por debajo del precio actual con volumen creciente.
Las posiciones están protegidas por un stop inicial y se acompañan con trailing stop a medida que el precio se mueve favorablemente.

## Detalles

- **Criterios de entrada**:
  - Largo: Colocar órdenes buy limit cada `SpaceBetweenTrades` pasos por debajo del bid.
  - Corto: Colocar órdenes sell limit cada `SpaceBetweenTrades` pasos por encima del ask.
- **Largo/Corto**: Opcional para cada lado mediante `TradeLong` y `TradeShort`.
- **Criterios de salida**: Trailing stop o ejecución del lado opuesto de la rejilla.
- **Stops**: `StopLossPips` inicial y trailing mediante `TrailingStopPips`.
- **Valores predeterminados**:
  - `StartingVolume` = 0.1m
  - `IncreasePercentage` = 1m
  - `SpaceBetweenTrades` = 700m
  - `NumberOfTrades` = 50
  - `StopLossPips` = 300m
  - `TrailingStopPips` = 100m
  - `TradeLong` = true
  - `TradeShort` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Trading en rejilla
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Trailing
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
