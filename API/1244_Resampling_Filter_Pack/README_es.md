# Estrategia de Paquete de Filtros de Remuestreo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia muestrea el precio cada N barras y lo suaviza con una media móvil. Va largo cuando el valor filtrado sube y el precio opera por encima de él, y va corto cuando el valor filtrado cae y el precio está por debajo.

## Detalles
- **Criterios de entrada**:
  - **Largo**: la pendiente del filtro es ascendente y el cierre está por encima del filtro.
  - **Corto**: la pendiente del filtro es descendente y el cierre está por debajo del filtro.
- **Criterios de salida**: señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `BarsPerSample` = 5
  - `MovingAverageType` = EMA
  - `MaPeriod` = 9
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: Media móvil
  - Complejidad: Simple
  - Nivel de riesgo: Medio
