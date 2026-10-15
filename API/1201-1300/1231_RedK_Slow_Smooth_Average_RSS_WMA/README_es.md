# Estrategia RedK de Promedio Lento y Suavizado RSS WMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Usa una media móvil ponderada de triple pasada para filtrar el ruido. Se abre una posición cuando el promedio suavizado cambia de dirección: largo cuando gira hacia arriba, corto cuando gira hacia abajo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: la pendiente del WMA triple gira hacia arriba.
  - **Corto**: la pendiente del WMA triple gira hacia abajo.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `CombinedSmoothness` = 15
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: WeightedMovingAverage
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
