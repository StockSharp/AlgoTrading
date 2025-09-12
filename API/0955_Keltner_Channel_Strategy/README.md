# Keltner Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades Keltner Channel breakouts and EMA trend crossings.

## Details

- **Entry Criteria**:
  - Long: price crosses below the lower Keltner band or EMA9 crosses above EMA21 while price is above EMA50.
  - Short: price crosses above the upper Keltner band or EMA9 crosses below EMA21 while price is below EMA50.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Price crosses the middle band in the opposite direction or the EMAs cross back.
  - Stop loss at 1.5 ATR.
  - Take profit at 3 ATR.
- **Stops**: Yes.
- **Default Values**:
  - `Length` = 20
  - `Multiplier` = 1.5
  - `AtrMultiplier` = 1.5
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `TrendEmaPeriod` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Channel
  - Direction: Both
  - Indicators: EMA, ATR, Keltner
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
