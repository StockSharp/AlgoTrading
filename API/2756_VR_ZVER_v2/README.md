# VR-ZVER v2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The VR-ZVER v2 strategy is a StockSharp port of the classic MetaTrader expert advisor. It keeps the triple confirmation idea of the original script: every trade must be supported by moving averages, the stochastic oscillator, and RSI. Only when all enabled filters agree does the strategy place a market order.

## Trading Logic

- Signals are evaluated when a candle closes. Intrabar fluctuations are only used to validate stops or targets.
- Three exponential moving averages (fast, slow, very slow) must be stacked in the same order to validate the trend when the MA filter is enabled.
- The stochastic filter waits for a %K/%D crossover near configurable upper and lower bands.
- The RSI filter requires the oscillator to leave a neutral zone (below the lower band for longs, above the upper band for shorts).
- A signal is accepted only when every enabled filter votes in the same direction. If any filter disagrees, nothing is traded.
- The strategy opens one position at a time. It does not hedge or build grids; when flat it waits for the next aligned signal.

## Position Management

- A take-profit and stop-loss are expressed in pips. The initial stop is set to two-thirds of the configured distance, reproducing the original EA behaviour.
- A breakeven trigger (also in pips) moves the stop to the entry price once the trade has gained the specified distance.
- Trailing stops use a distance and an additional step. The step prevents the stop from being updated on every small uptick and matches the MT5 trailing logic.
- Long and short trades share the same management rules and react symmetrically to highs/lows of the candle.

## Position Sizing

- `FixedVolume` greater than zero opens every order with a fixed size.
- When `FixedVolume` is set to zero, the strategy computes the volume from `RiskPercent`, the current portfolio value, and the stop distance. Price step and step price are used to convert the pip distance into monetary risk.
- Volumes are rounded to respect `VolumeMin`, `VolumeMax`, and `VolumeStep` constraints of the instrument. Orders are skipped if the calculated size is too small.

## Parameters

| Name | Description |
| ---- | ----------- |
| `CandleType` | Time frame used for signal generation (default 15-minute candles). |
| `FixedVolume`, `RiskPercent` | Choose between fixed or risk-based sizing. |
| `StopLossPips`, `TakeProfitPips` | Base protective distances in pips. |
| `TrailingStopPips`, `TrailingStepPips`, `BreakevenPips` | Trade management thresholds. |
| `AllowLongs`, `AllowShorts` | Enable or disable individual directions. |
| `UseMovingAverageFilter`, `FastMaPeriod`, `SlowMaPeriod`, `VerySlowMaPeriod` | Triple EMA trend filter. |
| `UseStochastic`, `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSmooth`, `StochasticUpperLevel`, `StochasticLowerLevel` | Stochastic confirmation settings. |
| `UseRsi`, `RsiPeriod`, `RsiUpperLevel`, `RsiLowerLevel` | RSI confirmation band. |

## Notes

- Pip conversion emulates the original EA: five- and three-digit symbols multiply the price step by ten before calculating pip values.
- The StockSharp port only uses market orders. The locking and pending order features of the MetaTrader version are intentionally omitted to keep the implementation consistent with the high-level API.
- Attach the strategy to a chart if you want to see the EMA, stochastic, and RSI overlays; they are drawn automatically when a chart area is available.
