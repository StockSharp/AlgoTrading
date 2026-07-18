# Estrategia de Media Móvil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra en una posición larga cuando una media móvil corta cruza por encima de una media móvil larga del tipo de precio seleccionado. La posición se cierra cuando la media corta vuelve a cruzar por debajo de la larga.

## Detalles
- **Criterios de entrada:** La MA corta cruza por encima de la MA larga.
- **Criterios de salida:** La MA corta cruza por debajo de la MA larga.
- **Indicadores:** SMA, EMA, DEMA, TEMA, WMA, VWMA.
- **Fuente de precio:** Close, High, Open, Low, Typical, Center.
- **Stops:** Ninguno.
- **Valores predeterminados:**
  - `MaType` = EMA
  - `ShortLength` = 1
  - `LongLength` = 20
  - `PriceType` = Typical
  - `CandleType` = 1 minute
- **Filtros:**
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: Media móvil
  - Stops: No
  - Complejidad: Simple
  - Nivel de riesgo: Medio
