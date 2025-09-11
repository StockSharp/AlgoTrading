# PresentTrend RMI Synergy
[Русский](README_ru.md) | [中文](README_cn.md)

PresentTrend RMI Synergy combines an RSI-based momentum filter with a SuperTrend-style ATR trailing stop. Entries occur when momentum exceeds thresholds and price is aligned with trend. The stop dynamically trails price using a moving average and ATR band.

Backtests show stable performance on trending markets like crypto.

## Details

- **Entry Criteria**: RMI above 60 with price above moving average for longs; RMI below 40 with price below moving average for shorts.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR-based trailing stop.
- **Stops**: Yes.
- **Default Values**:
  - `RmiPeriod` = 21
  - `SuperTrendLength` = 5
  - `SuperTrendMultiplier` = 4.0m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RSI, ATR, SMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

