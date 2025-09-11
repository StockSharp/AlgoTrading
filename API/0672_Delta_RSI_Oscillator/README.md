# Delta-RSI Oscillator
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Delta-RSI oscillator, defined as the change of RSI smoothed with an EMA. Signals are triggered when the delta crosses zero, crosses its signal line or changes direction. Exits mirror the selected condition.

## Details

- **Entry Criteria**: Based on `BuyCondition` (zero-crossing, signal line crossing or direction change) on Delta-RSI.
- **Long/Short**: Both, controlled by `UseLong` and `UseShort`.
- **Exit Criteria**: Based on `ExitCondition` on Delta-RSI.
- **Stops**: None.
- **Default Values**:
  - `RsiLength` = 21
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: RSI, EMA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
