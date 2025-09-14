# Last Price Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy places limit orders at the best bid or ask when the last trade price moves away by a user-defined interval. It listens to Level1 order book updates and trade prints to decide entries.

## Details

- **Entry Criteria**:
  - **Long**: Last price ≥ best ask + interval.
  - **Short**: Last price ≤ best bid - interval.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Opposite signal or outside allowed trading sessions.
- **Stops**: Stop loss only.
- **Default Values**:
  - `Interval` = 400
  - `Min Volume` = 1
  - `Max Volume` = 900000
  - `Spread` = 200
  - `Volume` = 1
  - `Stop Loss` = 400
- **Trading Sessions**:
  - 10:05:40 – 13:54:30
  - 14:08:30 – 15:44:30
  - 16:05:30 – 18:39:30
  - 19:15:10 – 23:44:30
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
