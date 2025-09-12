# RSI Divergence Strategy - AliferCrypto

Strategy based on RSI divergences with optional zone and trend filters. Stop loss and take profit can be calculated from swings or ATR with dynamic or static updates.

## Logic
- **Entry**
  - Bullish divergence: price makes lower low while RSI makes higher low.
  - Bearish divergence: price makes higher high while RSI makes lower high.
  - Optional RSI zone filter requires prior oversold/overbought state.
  - Optional trend filter uses moving average direction.
- **Exit**
  - SL/TP from recent swing or ATR.
  - Levels may be locked at entry or recalculated each bar.

## Indicators
- Relative Strength Index
- Moving Average
- Average True Range
- Highest/Lowest
