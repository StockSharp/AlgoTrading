# Anands Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This breakout system sets the trade direction using the previous day's candle.
If the prior close is above that day's high the strategy looks for longs, while a close below the low turns it bearish.
On the 15‑minute timeframe it watches the last two completed candles.
A long position opens when the previous candle closes above the high from two bars ago.
A short position opens when the previous close falls below the low from two bars back.

## Details

- **Entry Criteria**:
  - Previous day close above/below its range sets bullish/bearish bias.
  - **Long**: prior 15m close > high two bars ago.
  - **Short**: prior 15m close < low two bars ago.
- **Long/Short**: Both sides.
- **Exit Criteria**: Not defined, reverse signal closes.
- **Stops**: Suggested at opposite side of the breakout bar.
- **Default Values**:
  - `CandleType` = 15 minute
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Candles
  - Stops: Optional
  - Complexity: Low
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
