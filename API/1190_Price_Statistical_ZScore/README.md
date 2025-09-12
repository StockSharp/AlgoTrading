# Price Statistical Z-Score
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using smoothed Z-Score cross with a candle momentum filter.

It buys when the short-term Z-Score rises above the long-term Z-Score and closes when it falls below. The strategy ignores signals after several identical ones and avoids entries following three bullish candles.

## Details

- **Entry Criteria**: Short-term Z-Score above long-term, no prior bullish 3-bar sequence, gap between signals.
- **Long/Short**: Long only.
- **Exit Criteria**: Short-term Z-Score below long-term, no prior bearish 3-bar sequence, gap between signals.
- **Stops**: No.
- **Default Values**:
  - `ZScoreBasePeriod` = 3
  - `ShortSmoothPeriod` = 3
  - `LongSmoothPeriod` = 5
  - `GapBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: SMA, StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
