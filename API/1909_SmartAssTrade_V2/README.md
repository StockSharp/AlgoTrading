# SmartAssTrade V2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

SmartAssTrade V2 Strategy uses MACD histogram and 20-period moving averages across multiple timeframes (1m, 5m, 15m, 30m, 60m) combined with Williams %R and RSI filters to capture trend momentum. Optional trailing stop protects profits.

## Details

- **Entry Criteria**: majority of timeframes show rising MACD histogram and MA with supporting WPR/RSI confirmation
- **Long/Short**: Both
- **Exit Criteria**: price reaches take profit or stop loss; optional trailing stop
- **Stops**: Absolute stop loss and take profit with optional trailing
- **Default Values**:
  - `Volume` = 1
  - `TakeProfit` = 35
  - `StopLoss` = 62
  - `UseTrailingStop` = false
  - `TrailingStop` = 30
  - `TrailingStopStep` = 1
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD, SMA, Williams %R, RSI
  - Stops: Optional
  - Complexity: Intermediate
  - Timeframe: Multi-timeframe (1m,5m,15m,30m,60m)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
