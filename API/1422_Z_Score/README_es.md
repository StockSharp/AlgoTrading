# Estrategia Z-Score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia calcula el Z-Score de una EMA de Heikin-Ashi y opera basándose en cruces de umbrales dinámicos derivados de rangos recientes.

## Detalles

- **Criterios de entrada**: Score cruzando por encima del mínimo reciente o EMA del score cruzando por encima del rango medio
- **Largo/Corto**: Ambos
- **Criterios de salida**: EMA del score cruzando por debajo del máximo o mínimo reciente
- **Stops**: No
- **Valores predeterminados**:
  - `HaEmaLength` = 10
  - `ScoreLength` = 25
  - `ScoreEmaLength` = 20
  - `RangeWindow` = 100
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: EMA, SMA, StdDev, Highest, Lowest
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
