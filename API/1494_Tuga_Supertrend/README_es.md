# Tuga Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tuga Supertrend es una estrategia solo larga basada en el indicador SuperTrend. Entra en una posición larga cuando la dirección del SuperTrend cambia hacia abajo y sale cuando la dirección gira hacia arriba.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: La dirección del SuperTrend cambia de arriba hacia abajo dentro de la ventana de fechas.
- **Criterios de salida**: La dirección del SuperTrend cambia de abajo hacia arriba.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `AtrPeriod` = 10
  - `Factor` = 3.0
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: SuperTrend, ATR
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
