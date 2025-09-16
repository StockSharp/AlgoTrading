# Stochastic Martingale Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a classic Stochastic oscillator entry with a martingale style averaging.
It opens a position when the %K line crosses the %D line and the oscillator is above/below configurable zones.
If price moves against the position by a defined step, the strategy increases volume by a multiplier.
Positions are closed when accumulated profit reaches a defined number of points.

## Details
- **Entry Criteria**
  - Long: %K > %D and %D > ZoneBuy
  - Short: %K < %D and %D < ZoneSell
- **Averaging**
  - Additional orders are placed every `Step` points (or `Step * orders count` in mode 1).
  - Volume of each new order is multiplied by `Mult`.
- **Exit Criteria**
  - Long: price ≥ last buy price + `ProfitFactor * orders count` points.
  - Short: price ≤ last sell price − `ProfitFactor * orders count` points.
- **Parameters** include step size, step mode, profit factor, multiplier, initial volumes and stochastic periods.
- **Filters**
  - Category: Trend following
  - Direction: Both
  - Indicators: Stochastic
  - Stops: No
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
