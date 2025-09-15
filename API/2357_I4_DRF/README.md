# I4 DRF Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the custom I4 DRF indicator. It compares the direction of recent candle highs and lows and generates a value between -100 and +100. Trading actions depend on color transitions of this indicator and the selected mode.

## Details

- **Entry Criteria**:
  - Mode `Direct`: open long when the indicator changes from positive to negative, open short when it changes from negative to positive.
  - Mode `NotDirect`: open long on a change from negative to positive, open short on a change from positive to negative.
- **Long/Short**: Both
- **Exit Criteria**:
  - Positions are closed when the opposite signal appears.
- **Stops**: None
- **Default Values**:
  - `Period` = 11
  - `SignalBar` = 1
  - `TrendMode` = Direct
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: I4 DRF
  - Stops: No
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
