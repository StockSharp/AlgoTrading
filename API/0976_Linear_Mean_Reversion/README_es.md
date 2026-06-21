# Estrategia de Reversión a la Media Lineal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Reversión a la Media Lineal usa el z-score del precio relativo a una media móvil para operar la reversión a la media con un stop loss fijo en puntos.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: z-score < -EntryThreshold.
  - **Corto**: z-score > EntryThreshold.
- **Criterios de salida**: El z-score vuelve hacia cero (z-score > -ExitThreshold para largos, z-score < ExitThreshold para cortos).
- **Stops**: Stop loss fijo en puntos.
- **Valores predeterminados**:
  - `HalfLife` = 14
  - `Scale` = 1
  - `EntryThreshold` = 2
  - `ExitThreshold` = 0.2
  - `StopLossPoints` = 50
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo y Corto
  - Indicadores: SMA, StandardDeviation
  - Stops: Sí
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
