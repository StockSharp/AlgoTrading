# Donchian WMA Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Donchian channel low crossing above a weighted moving average triggers long entries only during calendar year 2025. Positions are closed when a take profit level is reached, the crossover reverses with a falling WMA, or the date exits 2025.

## Details

- **Entry Criteria**:
  - Long: `DonchianLow` crosses above `WMA` and date within 2025
- **Long/Short**: Long only
- **Exit Criteria**:
  - Take profit via `TakeProfitPercent`
  - Crossunder of `DonchianLow` below `WMA` while `WMA` declines
  - Date outside 2025
- **Stops**: Take profit only
- **Default Values**:
  - `DonchianLength` = 7
  - `WmaLength` = 62
  - `TakeProfitPercent` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: Donchian Channel, Weighted Moving Average
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Mid-term
  - Seasonality: Year 2025 only
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
