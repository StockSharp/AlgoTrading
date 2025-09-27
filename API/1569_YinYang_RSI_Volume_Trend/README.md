# YinYang RSI Volume Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

YinYang RSI Volume Trend Strategy uses volume-weighted price zones and an RSI filter to detect trend reversals. The strategy buys when price exits the lower zone and sells when it exits the upper zone. Optional stop-loss and take-profit levels are based on dynamic zones.

## Details

- **Entry Criteria**: Price crosses out of the calculated purchase zones with availability reset options.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price reaches the opposite zone or triggers optional stop-loss/take-profit.
- **Stops**: Optional.
- **Default Values**:
  - `TrendLength` = 80
  - `UseTakeProfit` = true
  - `UseStopLoss` = true
  - `StopLossMultiplier` = 0.1
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: VWMA, EMA, RSI
  - Stops: Optional
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
