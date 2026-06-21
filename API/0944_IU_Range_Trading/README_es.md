# Estrategia IU de Negociación en Rango
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia IU Range Trading identifica zonas de consolidación donde el rango de precios durante un período de lookback se mantiene dentro de un multiplicador ATR. Las operaciones de ruptura se activan cuando el precio supera los límites del rango. Las posiciones están protegidas por un stop trailing basado en ATR que se mueve con la acción favorable del precio.

## Detalles

- **Criterios de entrada**: El precio rompe por encima o por debajo de un rango estrecho definido por ATR.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop trailing basado en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RangeLength` = 10
  - `AtrLength` = 14
  - `AtrTargetFactor` = 2.0m
  - `AtrRangeFactor` = 1.75m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: ATR, Highest, Lowest
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
