# 2MA Bunny Cross Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **2MA Bunny Cross Expert** opera el cruce de dos medias móviles simples. Se abre una operación larga cuando la media rápida sube por encima de la lenta, mientras que se abre una operación corta cuando la media rápida cae por debajo de la lenta. Cualquier posición opuesta se cierra antes de abrir una nueva.

## Detalles

- **Propósito**: seguimiento de tendencia mediante el cruce de medias móviles
- **Operativa**: largo y corto
- **Indicadores**: Media móvil simple rápida y lenta
- **Stops**: ninguno
- **Valores predeterminados**:
  - `CandleType` = 1 minute
  - `FastLength` = 5
  - `SlowLength` = 20
