# Estrategia de Proyección
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula el cambio porcentual promedio de las aperturas diarias recientes y proyecta niveles de ruptura alrededor de la apertura del día actual. Las posiciones largas se abren cuando el precio rompe por encima de la proyección superior, mientras que las posiciones cortas se abren cuando rompe por debajo de la proyección inferior. Los stops de protección se colocan cerca del lado opuesto de la proyección.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio cruza por encima de `open + threshold`.
  - **Corto**: el precio cruza por debajo de `open - threshold`.
- **Criterios de salida**:
  - **Largo**: el precio cae por debajo del stop largo.
  - **Corto**: el precio sube por encima del stop corto.
- **Stops**: sí, basados en el cambio promedio.
- **Parámetros**:
  - `TargetMultiple` – multiplicador del cambio promedio (predeterminado 0.2).
  - `Threshold` – porcentaje del cambio promedio usado para formar los niveles de ruptura (predeterminado 1.0).
  - `CalculationPeriod` – número de días en el promedio (predeterminado 5).
