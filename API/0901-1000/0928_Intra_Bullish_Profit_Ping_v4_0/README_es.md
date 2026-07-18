# Estrategia Intra Bullish - Profit Ping v4.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema solo largos que utiliza un cruce de EMA confirmado por el histograma MACD y la fortaleza del RSI.

## Detalles

- **Criterios de entrada**:
  - La EMA corta cruza por encima de la EMA larga
  - Histograma MACD > 0
  - RSI > 50
  - Cierre > Apertura
- **Criterios de salida**:
  - La EMA corta cruza por debajo de la EMA larga
  - Histograma MACD < 0
  - RSI < 50
  - Cierre < Apertura
- **Indicadores**:
  - Medias Móviles Exponenciales
  - MACD
  - RSI
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `ShortEmaLength` = 7
  - `LongEmaLength` = 14
  - `RsiLength` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
- **Filtros**:
  - Seguimiento de tendencia
  - Marco temporal único
  - Indicadores: EMA, MACD, RSI
  - Stops: ninguno
  - Complejidad: Bajo
