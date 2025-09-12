# Trend Following Parabolic Buy Sell Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines Parabolic SAR with moving average crossovers.
Long entries occur when price is above a long trend SMA, the fast EMA crosses above the slow EMA, and SAR is bullish.
Short entries use the opposite conditions.
Stop loss is placed at the trend SMA and take profit uses a risk/reward ratio.

## Details

- **Entry**:
  - **Long**: price > trend SMA, fast EMA crosses above slow EMA, SAR bullish
  - **Short**: price < trend SMA, fast EMA crosses below slow EMA, SAR bearish
- **Exit**:
  - stop at trend SMA
  - take profit = risk/reward * distance from entry to trend SMA
- **Indicators**: Parabolic SAR, SMA, EMA
- **Timeframe**: configurable
- **Type**: Trend following
