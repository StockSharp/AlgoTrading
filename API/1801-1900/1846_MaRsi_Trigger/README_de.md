# MA RSI-Trigger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert schnelle und langsame exponentielle gleitende Durchschnitte (EMA) mit RSI, um Trendwenden zu erkennen.
Wenn der schnelle EMA und der schnelle RSI beide über ihren langsamen Gegenstücken liegen, wird der Markt als bullisch behandelt und eine Long-Position eröffnet.
Wenn beide darunter liegen, wird eine Short-Position eröffnet. Parameter ermöglichen das Aktivieren oder Deaktivieren von Long- und Short-Einstiegen oder -Ausstiegen.

## Details

- **Einstiegskriterien**:
  - **Long**: schneller EMA > langsamer EMA UND schneller RSI > langsamer RSI bei vorherigem bärischen Trend.
  - **Short**: schneller EMA < langsamer EMA UND schneller RSI < langsamer RSI bei vorherigem bullischen Trend.
- **Ausstiegskriterien**:
  - **Long**: Trend wird bärisch und Long-Ausstiege sind erlaubt.
  - **Short**: Trend wird bullisch und Short-Ausstiege sind erlaubt.
- **Indikatoren**: EMA, RSI.
- **Stops**: Nicht enthalten.
- **Zeitrahmen**: standardmäßig 4-Stunden-Kerzen.
- **Parameter**:
  - `FastRsiPeriod` = 3
  - `SlowRsiPeriod` = 13
  - `FastMaPeriod` = 5
  - `SlowMaPeriod` = 10
  - `AllowBuyEntry` = true
  - `AllowSellEntry` = true
  - `AllowLongExit` = true
  - `AllowShortExit` = true
