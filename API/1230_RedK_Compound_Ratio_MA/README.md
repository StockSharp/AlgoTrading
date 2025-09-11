# Redk Compound Ratio MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades long when the compound ratio moving average (CoRa Wave) rises and short when it falls.

## Details

- **Entry Criteria**:
  - Long: CoRa Wave value rises above previous value
  - Short: CoRa Wave value falls below previous value
- **Long/Short**: Both
- **Exit Criteria**:
  - Opposite signal
- **Stops**: None
- **Default Values**:
  - `Length` = 20
  - `RatioMultiplier` = 2m
  - `AutoSmoothing` = true
  - `ManualSmoothing` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Compound Ratio MA, Weighted Moving Average
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: None
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
