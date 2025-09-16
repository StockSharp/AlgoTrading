# Gap Fill
[Русский](README_ru.md) | [中文](README_cn.md)

Gap Fill strategy exploits price gaps between consecutive 15-minute candles.
When a new candle opens above the previous candle's high by more than a configurable threshold, the strategy sells and places a buy limit at the prior high, aiming for the gap to close.
When a candle opens below the previous low by more than the threshold, it buys and places a sell limit at the prior low.
The threshold is calculated as `MinGapSize` price steps plus the current spread between best bid and ask.

## Details

- **Entry Criteria**: Gap between current open and previous high/low exceeds `MinGapSize` plus spread.
- **Long/Short**: Both directions.
- **Exit Criteria**: Limit order at the previous candle extreme.
- **Stops**: No.
- **Default Values**:
  - `MinGapSize` = 1
  - `Volume` = 0.1
  - `CandleType` = 15 minutes
- **Filters**:
  - Category: Gap
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
