# StepMA NRTR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy based on the StepMA NRTR indicator. The indicator combines a step moving average with a Nick Rar Trend reversal mechanism and generates buy or sell signals when the trend changes.

## Details

- **Entry Criteria**: StepMA NRTR buy/sell signal
- **Long/Short**: both
- **Exit Criteria**: opposite StepMA NRTR signal
- **Stops**: none
- **Default Values**:
  - `Length` = 10
  - `Kv` = 1
  - `StepSize` = 0
  - `UseHighLow` = true
  - `CandleType` = TimeFrame 1 hour
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: StepMA NRTR
  - Stops: None
  - Complexity: Medium
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
