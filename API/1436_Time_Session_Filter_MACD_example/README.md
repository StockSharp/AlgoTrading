# Time Session Filter - MACD example
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy demonstrates using a time session filter with MACD and trend EMA. Trades only during the configured hours.

## Details

- **Entry Criteria**: MACD crosses signal within active session and price relative to trend EMA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover or session end when enabled.
- **Stops**: No.
- **Default Values**:
  - `SessionStart` = 11:00
  - `SessionEnd` = 15:00
  - `CloseAtSessionEnd` = false
  - `FastEmaPeriod` = 11
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `TrendMaLength` = 55
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD, EMA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
