# Cci Threshold Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that buys when CCI drops below a threshold and exits when the close price exceeds the previous close.
Optional stop loss and take profit in absolute points.

## Details

- **Entry Criteria**:
  - Long: `CCI < BuyThreshold`
- **Long/Short**: Long only
- **Exit Criteria**:
  - `ClosePrice > previous ClosePrice`
- **Stops**: Optional via `UseStopLoss` and `UseTakeProfit`
- **Default Values**:
  - `LookbackPeriod` = 12
  - `BuyThreshold` = -90
  - `StopLossPoints` = 100m
  - `TakeProfitPoints` = 150m
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: CCI
  - Stops: Optional
  - Complexity: Low
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
