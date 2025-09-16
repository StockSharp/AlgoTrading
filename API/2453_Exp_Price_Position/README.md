# Exp Price Position
[Русский](README_ru.md) | [中文](README_cn.md)

The **Exp Price Position** strategy adapts the original MetaTrader expert advisor that combines price location and a step trend filter.
It evaluates the relationship between two median moving averages to locate the most recent swing level and then checks a fast and slow smoothed moving average pair to determine trend direction.
Orders are opened only when both the price position and step trend agree with the current candle structure.

The strategy is designed for markets where trend shifts occur after price pulls back to a dynamic median level. A trailing stop and take-profit ratio are applied to manage risk.

## Details

- **Entry Criteria**: Price above last swing level with bullish step trend for long trades; below with bearish step trend for short trades.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or protective stop.
- **Stops**: Yes, via trailing stop with take-profit ratio.
- **Default Values**:
  - `FastPeriod` = 2
  - `SlowPeriod` = 30
  - `MedianFastPeriod` = 26
  - `MedianSlowPeriod` = 20
  - `TpSlRatio` = 3m
  - `TrailingStopPips` = 10m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: Smoothed Moving Average, Simple Moving Average
  - Stops: Trailing
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

