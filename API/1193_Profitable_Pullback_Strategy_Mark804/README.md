# Profitable Pullback Strategy Mark804
[Русский](README_ru.md) | [中文](README_cn.md)

A trend-following pullback strategy using a ribbon of exponential moving averages. The system looks for price retracements to the signal EMA within a confirmed trend. When price closes back in the trend direction after a pullback, the strategy opens a position and protects it with percentage-based take profit and stop loss levels.

## Details

- **Entry Criteria**:
  - **Long**: Fast EMA > Signal EMA > Medium EMA, optional Medium EMA > Slow EMA, previous close below Signal EMA and current close above.
  - **Short**: Fast EMA < Signal EMA < Medium EMA, optional Medium EMA < Slow EMA, previous close above Signal EMA and current close below.
- **Long/Short**: Both sides.
- **Exit Criteria**: Take profit or stop loss is hit.
- **Stops**: Yes, fixed take profit and stop loss percentages.
- **Default Values**:
  - Fast EMA Length = 8
  - Signal EMA Length = 21
  - Medium EMA Length = 50
  - Slow EMA Length = 200
  - Take Profit % = 2
  - Stop Loss % = 1
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Medium
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
