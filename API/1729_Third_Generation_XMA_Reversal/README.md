# 3rd Generation XMA Reversal
[Русский](README_ru.md) | [中文](README_cn.md)

Employs a double-smoothed exponential moving average known as the 3rd Generation XMA to spot local highs and lows. A long position is opened when the XMA turns upward from a local bottom. Shorts are initiated when the XMA reverses from a local top. Positions are reversed on opposite signals and no explicit stop or take profit is used.

## Details
- **Entry Criteria**: XMA forms a local minimum or maximum and reverses.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `MaLength` = 50
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (4H)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
