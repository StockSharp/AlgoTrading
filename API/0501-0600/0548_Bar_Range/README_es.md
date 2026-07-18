# Estrategia de Rango de Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Bar Range entra en largo cuando el rango de la barra actual está entre los más altos de las barras recientes y la vela cierra por debajo de su apertura. La posición se cierra después de un número fijo de barras.

## Detalles

- **Criterios de entrada**:
  - Rango = High − Low
  - Rango percentil sobre `LookbackPeriod` ≥ `PercentRankThreshold`
  - Close < Open
- **Criterios de salida**: Cerrar posición después de `ExitBars` barras.
- **Valores predeterminados**:
  - `LookbackPeriod` = 50
  - `PercentRankThreshold` = 95
  - `ExitBars` = 1
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Largo
  - Indicadores: Percent Rank
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
