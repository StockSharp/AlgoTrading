# Ozymandias Trend
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Ozymandias indicator. The indicator combines ATR with moving averages of highs and lows to build a dynamic channel. When direction switches from bearish to bullish, the strategy buys and closes short positions. A switch to bearish sells and closes longs. Optional take profit and stop loss parameters manage risk.

## Details

- **Entry Criteria**: Direction change of Ozymandias indicator.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or configured stops.
- **Stops**: Take profit and stop loss.
- **Default Values**:
  - `Length` = 2
  - `CandleType` = TimeSpan.FromHours(4)
  - `TakeProfitPoints` = 2000
  - `StopLossPoints` = 1000
  - `BuyEntry` = true
  - `SellEntry` = true
  - `BuyExit` = true
  - `SellExit` = true
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Ozymandias (ATR + MA)
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: 4h
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
