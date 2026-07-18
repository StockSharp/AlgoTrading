# Estrategia Dinámica de Ruptura Maestra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura que utiliza Canales de Donchian con filtro de tendencia de media móvil, filtros RSI y ATR más restricciones de volumen y tiempo.

## Reglas de la estrategia

- Largo: el precio rompe por encima de la banda superior de Donchian o retrocede después de la ruptura, MA1 > MA2, RSI entre `RsiOversold` y `RsiOverbought`, ATR por encima de `AtrMultiplier`, volumen por encima del promedio y dentro de las horas de operación.
- Corto: el precio rompe por debajo de la banda inferior de Donchian o retrocede después de la ruptura, MA1 < MA2, RSI entre los umbrales, ATR por encima de `AtrMultiplier`, volumen por encima del promedio y dentro de las horas de operación.
- Salidas: stop loss/trailing, toma de ganancias, RSI extremo o cruce de medias móviles.

## Parámetros

- `DonchianPeriod` – período de retroceso del canal.
- `Ma1Length`, `Ma1IsEma` – primera media móvil.
- `Ma2Length`, `Ma2IsEma` – segunda media móvil.
- `RsiLength`, `RsiOverbought`, `RsiOversold` – filtro RSI.
- `AtrLength`, `AtrMultiplier` – filtro de volatilidad.
- `RiskPerTrade`, `RewardRatio`, `AccountSize` – dimensionamiento de posición.
- `TradingStartHour`, `TradingEndHour` – sesión de operación.
- `CandleType` – marco temporal de velas.
