# Grim Slash
[Русский](README_ru.md) | [中文](README_cn.md)

Grim Slash is a simple price action strategy that buys when the current candle's low tests the previous close and exits when the high reaches the previous high. Risk is managed with fixed percentage take profit and stop loss.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Current low touches or dips below the previous close.
- **Exit Criteria**: Current high touches or exceeds the previous high.
- **Stops**: 15% take profit, 5% stop loss.
- **Default Values**:
  - `TakeProfitPercent` = 15
  - `StopLossPercent` = 5
- **Filters**:
  - Category: Reversal
  - Direction: Long
  - Indicators: None
  - Complexity: Low
  - Risk level: Medium
