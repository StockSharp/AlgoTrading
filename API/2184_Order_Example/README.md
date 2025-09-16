# Order Example Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy converted from the MQL5 sample `OrderExample.mq5`.
It enters trades when price breaks above recent highs or below recent lows.

The strategy uses the `Highest` and `Lowest` indicators to track breakout levels over a configurable window.

## Details

- **Entry Criteria**:
  - Long: `Close` breaks above highest high of `Lookback` candles
  - Short: `Close` breaks below lowest low of `Lookback` candles
- **Long/Short**: Both
- **Exit Criteria**: Opposite breakout
- **Stops**: No
- **Default Values**:
  - `Lookback` = 26
  - `CandleType` = `TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
