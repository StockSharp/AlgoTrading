# Entry Fragger Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy tracks sequences of red and green candles relative to the 50-period EMA. After a series of red candles below the EMA, a green candle closing above a volatility cloud triggers a long entry. A similar setup with green candles precedes short entries. Optional reverse trading enables flipping positions.

## Details

- **Entry Criteria**:
  - **Long**: `redCount >= Buy Signal Accuracy` && last red below EMA50 && green candle closes above `EMA50 + stdev/4`.
  - **Short**: `greenCount >= Sell Signal Accuracy` && previous candle green && red candle closes above `EMA50 + stdev/4`.
- **Long/Short**: Both sides.
- **Exit Criteria**: Reverse signal.
- **Indicators**: EMA, StandardDeviation.
- **Default Values**:
  - `Buy Signal Accuracy` = 2
  - `Sell Signal Accuracy` = 2
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Multiple
  - Stops: No
  - Complexity: Medium
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
