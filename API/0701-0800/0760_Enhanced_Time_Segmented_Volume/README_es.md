# Volumen Segmentado por Tiempo Mejorado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Volumen Segmentado por Tiempo Mejorado monitorea los cambios de precio ponderados por volumen. Cuando el TSV está por encima de su media móvil y es positivo, la estrategia compra. Cuando el TSV está por debajo del promedio y es negativo, vende en corto.

## Detalles

- **Criterios de entrada**: TSV en relación con su media móvil.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `TsvLength` = 13
  - `MaLength` = 7
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Volumen, SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
