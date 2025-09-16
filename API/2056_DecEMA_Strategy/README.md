# DecEMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using the DecEMA indicator to follow trend direction. The indicator applies ten consecutive exponential smoothings and combines them to create a low-lag moving average. The strategy compares the last three DecEMA values. If the line turns upward and the latest value exceeds the previous one, it buys and closes any short position. If the line turns downward and the latest value is below the previous one, it sells and closes any long position.

## Details

- **Entry Criteria**:
  - Long: DecEMA slope turns up and current value > previous value
  - Short: DecEMA slope turns down and current value < previous value
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: slope turns down
  - Short: slope turns up
- **Stops**: None
- **Default Values**:
  - `EmaPeriod` = 3
  - `Length` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: DecEMA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
