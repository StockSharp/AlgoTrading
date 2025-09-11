# Elliott Wave Supertrend Exit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that enters on ZigZag-like reversals and exits on Supertrend direction changes with a fixed percentage stop-loss.

## Details

- **Entry Criteria**:
  - Long: price forms a local low
  - Short: price forms a local high
- **Long/Short**: Both
- **Exit Criteria**:
  - Supertrend direction flip or stop-loss level
- **Stops**: Fixed percentage from entry price
- **Default Values**:
  - `WaveLength` = 4
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3
  - `StopLossPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Highest, Lowest, SuperTrend
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
