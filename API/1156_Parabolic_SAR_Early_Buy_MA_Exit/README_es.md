# Estrategia Parabolic SAR de Compra Anticipada con Salida por MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera reversiones del Parabolic SAR y cierra posiciones largas anticipadamente cuando el SAR gira por encima del precio y el cierre queda por debajo de una media móvil de N períodos.

## Detalles

- **Criterios de entrada**:
  - Cruce del precio con el Parabolic SAR.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Para posiciones largas: SAR por encima del precio y cierre por debajo de la MA (`MaPeriod`).
  - Para posiciones cortas: cruce inverso del SAR (gestionado por la lógica de entrada).
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `MaPeriod` = 11
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y corto
  - Indicadores: Parabolic SAR, SMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
