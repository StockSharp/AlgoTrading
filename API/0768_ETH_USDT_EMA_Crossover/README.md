# ETH/USDT EMA Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades ETH/USDT using an EMA crossover with additional filters.

A long position is opened when the 20-period EMA crosses above the 50-period EMA while price is above the 200-period EMA, RSI is above 30, volatility measured by ATR is above its moving average, and volume is greater than its average. A short position is opened on the opposite conditions.

Positions reverse when the opposite signal appears. No explicit stop-loss or take-profit is used.

## Details

- **Entry Criteria**:
  - **Long**: `EMA20 crosses above EMA50` && `Close > EMA200` && `RSI > 30` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
  - **Short**: `EMA20 crosses below EMA50` && `Close < EMA200` && `RSI < 70` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
- **Long/Short**: Both sides
- **Exit Criteria**:
  - Reverse signal
- **Stops**: No
- **Default Values**:
  - `EMA200 Length` = 200
  - `EMA20 Length` = 20
  - `EMA50 Length` = 50
  - `RSI Length` = 14
  - `ATR Length` = 14

- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, RSI, ATR
  - Stops: No
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
