# Bruno Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Bruno expert advisor is a trend-following system originally written for MetaTrader 5. The port keeps the same confirmation chain: Average Directional Index (ADX) with directional movement, a pair of exponential moving averages (EMA 8/21), MACD (13, 34, 8), a Stochastic Oscillator (21, 3, 3) and the slope of a Parabolic SAR (0.055, 0.21). Every filter that agrees with the direction multiplies the order size by a configurable factor. If both long and short signals are amplified on the same candle, trading is skipped to avoid conflicting orders.

### Trading logic

- **Directional bias**
  - Long pressure is strengthened when `+DI > -DI` and `+DI > 20`.
  - Short pressure is strengthened when `+DI < -DI` and `+DI < 40`.
- **Momentum alignment**
  - Long preference requires EMA(8) above EMA(21), Stochastic %K above %D and %K below the overbought threshold (default 80).
  - Short preference requires EMA(8) below EMA(21), Stochastic %K below %D and %K above the oversold threshold (default 20).
- **MACD filter**
  - Long bias: MACD histogram above zero and MACD main line above the signal line.
  - Short bias: MACD histogram below zero and MACD main line below the signal line.
- **Parabolic SAR slope**
  - Long bias is reinforced when the previous SAR values are rising while EMA(8) > EMA(21).
  - Short bias is reinforced when the previous SAR values are falling while EMA(8) < EMA(21).

Each satisfied condition multiplies the base lot size by `SignalMultiplier` (default 1.6). Only one side may be active at a time. When a final signal is generated, the strategy closes any opposite position, submits the market order with the multiplied volume, and stores the current close as the entry price.

### Position management

- **Stop-loss / take-profit** – fixed distances expressed in adjusted pips, matching the MetaTrader version. If either level is hit intrabar the position is closed immediately.
- **Trailing stop** – activates once floating profit exceeds `TrailingStop + TrailingStep` pips. The stop is then pulled behind price by `TrailingStop` pips and only advances when the gain increases by at least `TrailingStep` more pips.
- **Conflict handling** – if both long and short filters fire on the same candle, no new trade is taken.

### Parameters

| Group | Name | Description |
| --- | --- | --- |
| Trading | `BaseVolume` | Initial lot size before multipliers. |
| Trading | `SignalMultiplier` | Volume multiplier applied by each agreeing filter. |
| Risk Management | `StopLossPips` / `TakeProfitPips` | Protective distances in adjusted pips. Set to zero to disable. |
| Risk Management | `TrailingStopPips` / `TrailingStepPips` | Trailing distance and minimum step in adjusted pips. |
| Indicators | `AdxPeriod`, `AdxPositiveThreshold`, `AdxNegativeThreshold` | ADX length and DI thresholds. |
| Indicators | `FastEmaPeriod`, `SlowEmaPeriod` | EMA lengths used in trend confirmation. |
| Indicators | `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD configuration. |
| Indicators | `StochasticPeriod`, `StochasticKsmoothing`, `StochasticDsmoothing`, `StochasticOverbought`, `StochasticOversold` | Stochastic oscillator settings. |
| General | `CandleType` | Timeframe used for the entire signal chain (default 1 hour). |

### Notes

- Adjusted pip size follows the MetaTrader convention: instruments with 3 or 5 decimal digits are multiplied by 10.
- Parabolic SAR operates with acceleration step `0.055` and maximum `0.21`, mirroring the expert advisor defaults.
- The port keeps the original money-management style (volume stacking) but aggregates the exposure into a single StockSharp position.
