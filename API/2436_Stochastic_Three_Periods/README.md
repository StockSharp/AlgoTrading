# Stochastic Three Periods
[Русский](README_ru.md) | [中文](README_cn.md)

The **Stochastic Three Periods** strategy aligns fast stochastic signals with confirmation from two higher timeframes. Trades are opened when the fast oscillator crosses while both higher timeframes agree.

## Details

- **Entry Criteria**: Fast %K crosses %D with opposite reading `ShiftEntrance` bars ago; both higher timeframe stochastics show %K above %D; close price must move in signal direction.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite fast stochastic cross measured on previous candle.
- **Stops**: Fixed stop-loss and take-profit in points via `StartProtection`.
- **Default Values**:
  - `CandleType1` = 5m
  - `CandleType2` = 15m
  - `CandleType3` = 30m
  - `KPeriod1` = 5
  - `KPeriod2` = 5
  - `KPeriod3` = 5
  - `KExitPeriod` = 5
  - `ShiftEntrance` = 3
  - `TakeProfitPoints` = 30
  - `StopLossPoints` = 10
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Stochastic
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
