# Enhanced Bollinger Bands SL TP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades Bollinger Band bounces using limit orders and fixed pip-based stop loss and take profit.

## Details

- **Entry Criteria**:
  - Long: previous close <= previous lower band and close > lower band
  - Short: previous close >= previous upper band and close < upper band
- **Long/Short**: Both
- **Stops**: Absolute take profit and stop loss in pips
- **Default Values**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2m
  - `EnableLong` = true
  - `EnableShort` = true
  - `PipValue` = 0.0001m
  - `StopLossPips` = 10m
  - `TakeProfitPips` = 20m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Bollinger Bands
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
