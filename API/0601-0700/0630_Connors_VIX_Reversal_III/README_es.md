# Connors VIX Reversión III
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia contraria que utiliza picos del VIX relativos a su media móvil. Compra cuando el VIX salta por encima del promedio en un porcentaje establecido y vende en corto cuando el VIX cae por debajo de él.

Las posiciones se cierran cuando el VIX cruza la media móvil del día anterior.

## Detalles

- **Criterios de entrada**: VIX bajo por encima de la MA y cierre por encima de la MA en el umbral para compras; VIX alto por debajo de la MA y cierre por debajo del umbral para ventas.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: VIX cruzando la MA de ayer.
- **Stops**: No.
- **Valores predeterminados**:
  - `LengthMA` = 10
  - `PercentThreshold` = 10m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Contrario
  - Dirección: Ambos
  - Indicadores: VIX, SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
