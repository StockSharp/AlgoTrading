[Русский](README_ru.md) | [中文](README_cn.md)

RSI Long Position buys when RSI crosses above an oversold level and closes when RSI exceeds a take profit level or drops below a stop level.

## Details

- **Entry Criteria**: RSI crosses above `Oversold`
- **Long/Short**: Long
- **Exit Criteria**: RSI greater than `TakeProfit` or RSI crosses below `StopLoss`
- **Stops**: No
- **Default Values**:
  - `RsiLength` = 14
  - `Oversold` = 35
  - `TakeProfit` = 55
  - `StopLoss` = 30
- **Filters**:
  - Category: Oscillator
  - Direction: Long
  - Indicators: RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
