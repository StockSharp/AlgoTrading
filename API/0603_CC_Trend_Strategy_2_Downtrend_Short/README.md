# CC Trend Strategy 2 Downtrend Short
[Русский](README_ru.md) | [中文](README_cn.md)

Short-only strategy that sells when the previous close is below a dynamic Fibonacci high and EMA21 is below EMA55. Exits when the price crosses above EMA200 with non-negative profit or when the previous close rises above the 0.236 Fibonacci level and no new short signal appears.

## Details

- **Entry Criteria**:
  - Short: previous close below Fibonacci high and EMA21 below EMA55
- **Long/Short**: Short
- **Exit Criteria**:
  - Price crosses above EMA200 with profit
  - Previous close above 0.236 Fibonacci level without new short signal
- **Stops**: None
- **Default Values**:
  - `FibLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Short
  - Indicators: EMA, Fibonacci
  - Stops: No
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
