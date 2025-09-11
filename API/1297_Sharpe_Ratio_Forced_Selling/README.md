# Sharpe Ratio Forced Selling
[Русский](README_ru.md) | [中文](README_zh.md)

The Sharpe Ratio Forced Selling strategy goes long when the rolling Sharpe ratio drops below a negative threshold and exits when it rises above a positive threshold or the holding period exceeds a limit. Returns can be computed using logarithmic or simple changes and adjusted by a risk-free rate.

## Details

- **Entry Criteria**: Sharpe ratio below `EntrySharpeThreshold`.
- **Long/Short**: Long only.
- **Exit Criteria**: Sharpe ratio above `ExitSharpeThreshold` or holding period exceeds `MaxHoldingDays`.
- **Stops**: No.
- **Default Values**:
  - `Length` = 8
  - `EntrySharpeThreshold` = -5
  - `ExitSharpeThreshold` = 13
  - `MaxHoldingDays` = 80
  - `UseLogReturns` = true
  - `RiskFreeRateAnnual` = 0
  - `PeriodsPerYear` = 252
- **Filters**:
  - Category: Mean Reversion
  - Direction: Long
  - Indicators: Sharpe Ratio
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
