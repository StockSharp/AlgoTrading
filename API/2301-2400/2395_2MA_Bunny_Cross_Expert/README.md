# 2MA Bunny Cross Expert
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The **2MA Bunny Cross Expert** strategy trades the crossover of two simple moving averages. A long trade is opened when the fast average moves above the slow one, while a short trade is opened when the fast average drops below the slow one. Any opposite position is closed before a new one is opened.

## Details

- **Purpose**: trend following via moving average crossover
- **Trading**: long and short
- **Indicators**: fast and slow Simple Moving Average
- **Stops**: none
- **Default Values**:
  - `CandleType` = 1 minute
  - `FastLength` = 5
  - `SlowLength` = 20
