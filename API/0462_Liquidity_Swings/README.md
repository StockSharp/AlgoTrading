# Liquidity Swings Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Liquidity Swings strategy tracks recent pivot highs and lows to define resistance and support. A long trade occurs when the low crosses above support while the close remains below resistance. A short trade triggers when the high crosses below resistance while the close stays above support. Risk management uses a stop loss below/above the level with a buffer and a take profit at twice that distance, yielding a 1:2 risk-reward.

## Details

- **Entry Criteria**:
  - **Long**: Low crosses above support and close < resistance.
  - **Short**: High crosses below resistance and close > support.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Stop loss at level or buffered value.
  - Take profit at 2× risk distance.
- **Stops**: Stop loss and take profit.
- **Default Values**:
  - `Lookback` = 5
  - `StopLossBuffer` = 0.5
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Pivot highs/lows
  - Stops: Yes
  - Complexity: Low
  - Timeframe: 1h (default)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
