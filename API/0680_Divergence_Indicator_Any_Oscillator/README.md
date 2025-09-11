# Divergence Indicator (Any Oscillator)

Detects regular and hidden divergences between price and RSI. The strategy buys on bullish divergences and sells on bearish divergences.

## Parameters
- **Pivot Left** – bars to the left of pivot
- **Pivot Right** – bars to the right of pivot
- **Min Range** – minimum bars between pivots
- **Max Range** – maximum bars between pivots
- **RSI Length** – RSI period
- **Candle Type** – candle series type

## Indicator
- RSI

## Rules
- **Entry**:
  - Buy when price makes a lower low while RSI makes a higher low (bullish divergence)
  - Sell when price makes a higher high while RSI makes a lower high (bearish divergence)
  - Hidden divergences trade in the opposite direction
