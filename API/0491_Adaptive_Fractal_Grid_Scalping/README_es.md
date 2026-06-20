# Scalping de Cuadrícula Fractal Adaptativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Scalping de Cuadrícula Fractal Adaptativa coloca órdenes limitadas alrededor de pivotes fractales recientes utilizando el ATR para la distancia. La tendencia se define mediante una media móvil simple. Cuando la volatilidad supera un umbral, se colocan límites de compra por debajo de los mínimos fractales en tendencias alcistas y límites de venta por encima de los máximos fractales en tendencias bajistas. Las salidas se producen en el nivel de cuadrícula opuesto o con un stop trailing basado en ATR.

## Detalles

- **Criterios de entrada**: ATR por encima del umbral con el precio relativo a la SMA; límite de compra en el mínimo fractal menos el multiplicador ATR o límite de venta en el máximo fractal más el multiplicador ATR.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Nivel de cuadrícula opuesto o stop basado en fractales.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AtrLength` = 14
  - `SmaLength` = 50
  - `GridMultiplierHigh` = 2.0m
  - `GridMultiplierLow` = 0.5m
  - `TrailStopMultiplier` = 0.5m
  - `VolatilityThreshold` = 1.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Scalping
  - Dirección: Ambos
  - Indicadores: Fractal, ATR, SMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
