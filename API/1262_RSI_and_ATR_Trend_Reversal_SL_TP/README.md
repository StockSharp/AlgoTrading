# RSI and ATR Trend Reversal SL TP
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using RSI and ATR to detect trend reversals with dynamic stop-loss and take-profit levels.

## Details

- **Entry Criteria**: Price crossing adaptive RSI/ATR threshold.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite crossover.
- **Stops**: Integrated through dynamic threshold.
- **Default Values**:
  - `RsiLength` = 8
  - `RsiMultiplier` = 1.5
  - `Lookback` = 1
  - `MinDifference` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RSI, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
