# Puria-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Puria ist eine Trendfolge-Strategie, die einen schnellen EMA, zwei langsame LWMAs des Tiefpreises und einen MACD-Filter kombiniert. Eine Long-Position wird eröffnet, wenn der 5-Perioden-EMA über beiden 75- und 85-Perioden-LWMAs liegt, der vorherige Schlusskurs über dem EMA liegt und die MACD-Linie positiv ist. Eine Short-Position wird eröffnet, wenn die entgegengesetzten Bedingungen erfüllt sind. Die Strategie verwendet feste Take-Profit- und Stop-Loss-Levels und erlaubt nur eine Position pro Richtung bis ein entgegengesetztes Signal erscheint.

## Details
- **Einstiegskriterien**: EMA(5) über LWMA(75) und LWMA(85), vorheriger Schlusskurs über EMA, MACD(15,26) > 0 für Longs; umgekehrt für Shorts.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Feste Stop-Loss- und Take-Profit-Abstände in Preispunkten.
- **Standardwerte**:
  - `StopLoss` = 14
  - `TakeProfit` = 15
  - `Ma1Period` = 75
  - `Ma2Period` = 85
  - `Ma3Period` = 5
  - `CandleType` = 1-Minuten-Zeitrahmen
- **Filter**: MACD-Nulllinien-Filter.
