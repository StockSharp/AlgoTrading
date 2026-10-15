# Estrategia X Trail 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el cruce de dos medias móviles configurables calculadas a partir de un tipo de precio elegido.

## Detalles
- **Entrada**: Compra cuando MA1 cruza por encima de MA2 y este cruce es confirmado por las dos barras anteriores; vende cuando ocurre lo contrario.
- **Salida**: Cruce opuesto.
- **Indicadores**: Dos medias móviles con tipo seleccionable (simple, exponential, smoothed, weighted) y fuente de precio (close, open, high, low, median, typical, weighted).
- **Parámetros**:
  - `Ma1Length` = 1
  - `Ma1Type` = MovingAverageTypeEnum.Simple
  - `Ma1PriceType` = AppliedPriceType.Median
  - `Ma2Length` = 14
  - `Ma2Type` = MovingAverageTypeEnum.Simple
  - `Ma2PriceType` = AppliedPriceType.Median
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
