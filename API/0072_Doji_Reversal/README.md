# Doji Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Doji candles reflect a temporary balance of buyers and sellers. When a doji appears after a strong directional move it can precede a reversal as momentum fades. This strategy measures the candle body relative to its range to decide if a true doji formed.

Once a doji is detected, the previous candles are checked for an uptrend or downtrend. A doji following a decline may trigger a long entry while one after a rise can open a short. Stops are placed at a percentage distance from the entry and exits occur if price breaks beyond the doji's extremes.

The method aims to capture the first reaction away from the doji and is best suited for intraday charts where quick reversals often unfold.

## Details

- **Entry Criteria**: Doji candle after a directional move.
- **Long/Short**: Both.
- **Exit Criteria**: Price moving beyond doji high/low or stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `CandleType` = 5 minute
  - `DojiThreshold` = 0.1
  - `StopLossPercent` = 1
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
