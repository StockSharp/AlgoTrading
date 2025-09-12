# Long-Leg Doji Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Long-Leg Doji Breakout strategy identifies long-legged doji candles and trades breakouts above or below the doji range. An optional ATR filter ensures wicks are sufficiently long.

## Details

- **Entry Criteria**:
  - **Long**: Waiting for breakout && close > doji high && previous close <= doji high.
  - **Short**: Waiting for breakout && close < doji low && previous close >= doji low.
- **Long/Short**: Both sides.
- **Exit Criteria**: Close crosses SMA(20) opposite the position.
- **Stops**: None.
- **Default Values**:
  - `Doji body threshold %` = 0.1
  - `Minimum wick ratio` = 2
  - `Use ATR filter` = true
  - `ATR period` = 14
  - `ATR multiplier` = 0.5
- **Filters**:
  - Category: Pattern breakout
  - Direction: Both
  - Indicators: ATR, SMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
