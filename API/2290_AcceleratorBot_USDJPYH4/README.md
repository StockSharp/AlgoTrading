# AcceleratorBot USDJPY H4 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The AcceleratorBot strategy is a conversion of the original MQL4 expert designed for USDJPY on the H4 timeframe. It blends trend strength from the Average Directional Index (ADX), momentum from the Stochastic Oscillator and multi-timeframe Acceleration/Deceleration (AC) values. Candlestick patterns are used as directional filters.

## Details

- **Entry Criteria**: Trend or momentum signals confirmed by candlestick filters.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal, stop loss, take profit or trailing stop.
- **Stops**: Fixed and trailing.
- **Default Values**:
  - `StopLossPoints` = 750
  - `TakeProfitPoints` = 9999
  - `TrailPoints` = 0
  - `AdxPeriod` = 14
  - `AdxThreshold` = 20m
  - `X1` = 0
  - `X2` = 150
  - `X3` = 500
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend and momentum
  - Direction: Both
  - Indicators: ADX, Stochastic, AC
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: H4
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
