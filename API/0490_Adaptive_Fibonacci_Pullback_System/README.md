# Adaptive Fibonacci Pullback Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy averages three SuperTrend lines built with Fibonacci multipliers (0.618, 1.618, 2.618) and smooths the result with an EMA. Trades follow pullbacks to this adaptive trend while an AMA-based midline and optional RSI filter confirm direction.

## Details

- **Entry Criteria**:
  - Low below averaged SuperTrend and close above its smoothed value.
  - Previous close relative to AMA midline defines pullback.
  - **Long**: close above midline and RSI > threshold.
  - **Short**: close below midline and RSI < threshold.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Close crossing the smoothed SuperTrend in opposite direction.
- **Stops**: Percent stop loss and take profit via `StartProtection`.
- **Default Values**:
  - `AtrPeriod` = 8
  - `SmoothLength` = 21
  - `AmaLength` = 55
  - `RsiLength` = 7
  - `RsiBuy` = 70
  - `RsiSell` = 30
  - `TakeProfitPercent` = 5
  - `StopLossPercent` = 0.75
- **Filters**:
  - Category: Trend pullback
  - Direction: Both
  - Indicators: SuperTrend, EMA, AMA, RSI
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
