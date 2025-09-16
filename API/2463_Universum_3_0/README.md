# Universum 3.0 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

DeMarker oscillator based strategy that opens positions on each completed bar and adjusts volume using a martingale scheme.

## Details

- **Entry Criteria**:
  - Long: `DeMarker > 0.5`
  - Short: `DeMarker < 0.5`
- **Long/Short**: Both
- **Exit Criteria**:
  - Positions are closed by take profit or stop loss
- **Stops**: Absolute points via `TakeProfitPoints` and `StopLossPoints`
- **Default Values**:
  - `DemarkerPeriod` = 10
  - `TakeProfitPoints` = 50m
  - `StopLossPoints` = 50m
  - `InitialVolume` = 1m
  - `LossesLimit` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: DeMarker
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
