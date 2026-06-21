# Estrategia de Spread de Alto Rendimiento con Filtro SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera según el Spread de Alto Rendimiento o el índice VIX. Se abre una posición cuando el spread elegido cruza un umbral y un filtro de precio opcional lo confirma. El filtro de precio requiere que el cierre esté por encima de una media móvil simple para largos, o por debajo para cortos. Las posiciones se cierran después de un número fijo de barras.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Spread > umbral y cierre > SMA (si habilitado).
  - **Corto**: Spread < umbral y cierre < SMA (si habilitado).
- **Largo/Corto**: Ambos, seleccionado mediante parámetro.
- **Criterios de salida**:
  - Cerrar posición después de las barras del período de mantenimiento.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Threshold` = 5
  - `HoldingPeriod` = 5
  - `SmaLength` = 50
- **Filtros**:
  - Categoría: Macro
  - Dirección: Ambos
  - Indicadores: High Yield Spread/VIX, SMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: 1d (predeterminado)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
