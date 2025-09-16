# DayTrading Strategy (MetaTrader Conversion)

## Overview

The **DayTrading Strategy** is a faithful C# conversion of the classic MetaTrader 4 expert advisor "DayTrading" released by NazFunds in 2005. The original robot was designed for 5-minute Forex charts and combines multiple momentum and trend-following indicators to capture short-term directional moves with a modest fixed target and optional trailing stop. This StockSharp implementation reproduces the core decision logic while exposing every important threshold as a strategy parameter so that it can be optimized or adapted to different instruments.

## Indicator Stack

The strategy evaluates four indicators on the selected candle series:

- **Parabolic SAR** (`ParabolicSar`) with configurable acceleration, increment, and cap. It defines the baseline trend direction and has to flip below/above price to enable new entries.
- **MACD (12, 26, 9)** (`MovingAverageConvergenceDivergenceSignal`). The MACD line must be below the signal line for longs and above it for shorts, mirroring the original histogram/signal comparison in MQL.
- **Stochastic Oscillator (5, 3, 3)** (`StochasticOscillator`). The %K line must stay under 35 for longs and above 60 for shorts to ensure the market is coming out of an oversold/overbought zone.
- **Momentum (14)** (`Momentum`). A value below 100 unlocks long trades, whereas a value above 100 authorizes shorts, exactly as in the MT4 script.

All indicators are processed through the high-level `BindEx` pipeline, so no manual buffer management or historical indexing is required.

## Trading Rules

### Entry Conditions

A **long** position is opened when all of the following are true on the latest finished candle:

1. The Parabolic SAR dot prints at or below the current ask price **and** the previous dot was above the current dot (fresh SAR flip to bullish).
2. Momentum is below 100.
3. The MACD line is below its signal line.
4. Stochastic %K is below 35.

A **short** position is opened when the symmetric conditions are satisfied:

1. The Parabolic SAR dot prints at or above the current bid price **and** the previous dot was below the current dot (bearish flip).
2. Momentum is above 100.
3. The MACD line is above its signal line.
4. Stochastic %K is above 60.

Only one position can be open at a time. Whenever an opposite signal appears, the existing position is closed and no re-entry happens on the same candle—just like in the MetaTrader implementation where the `OrdersTotal` scan prevents immediate reloading.

### Exit Management

- **Stop Loss / Take Profit:** Optional fixed distances (in points) are converted to absolute prices using the instrument's tick size. They are re-evaluated on every candle and close the position if breached intrabar.
- **Trailing Stop:** Once price advances by the configured number of points, a trailing stop is activated. For long trades the stop trails below the close; for short trades it trails above the close. The stop never steps backwards, so profit is locked progressively.
- **Opposite Signal:** A valid opposite setup immediately liquidates the current position before any new entry is considered.

No additional grid, scaling, or hedging logic is added; the strategy stays as lightweight and deterministic as the original EA.

## Parameters

| Parameter | Default | Description |
| --- | --- | --- |
| `LotSize` | 1 | Volume of each market order. The `Strategy.Volume` property is synchronized to this value during start-up. |
| `TrailingStopPoints` | 15 | Trailing distance in points. Set to zero to disable trailing. |
| `TakeProfitPoints` | 20 | Fixed take-profit distance in points. Set to zero to remove the target. |
| `StopLossPoints` | 0 | Protective stop distance in points. Zero reproduces the original "no stop" behaviour. |
| `SlippagePoints` | 3 | Maximum execution slippage placeholder (for compatibility with the MT4 input). Not enforced automatically but kept for completeness. |
| `CandleType` | 5-minute time frame | Candle series used by all indicators. Keep at M5 to match the EA's original recommendation. |
| `MacdFastPeriod` | 12 | Fast EMA length in the MACD calculation. |
| `MacdSlowPeriod` | 26 | Slow EMA length in the MACD calculation. |
| `MacdSignalPeriod` | 9 | Signal EMA length in the MACD calculation. |
| `StochasticLength` | 5 | %K look-back length for the Stochastic Oscillator. |
| `StochasticSignal` | 3 | %D smoothing length. |
| `StochasticSlow` | 3 | Additional slowing applied to the %K line. |
| `MomentumPeriod` | 14 | Momentum look-back length. |
| `SarAcceleration` | 0.02 | Initial acceleration factor for Parabolic SAR. |
| `SarStep` | 0.02 | Increment applied to the acceleration factor after each new extreme. |
| `SarMaximum` | 0.2 | Maximum acceleration factor for Parabolic SAR. |

All numeric parameters can be optimized through StockSharp's optimization workflow thanks to the `SetCanOptimize(true)` hints.

## Implementation Notes

- Bid/ask prices are derived from live Level1 data when available; otherwise the candle close acts as a fallback so that the logic remains robust in historical testing.
- Point conversion relies on the instrument's `Step`/`PriceStep`. If none is provided a conservative `0.0001` fallback is used, which matches a standard Forex pip.
- Position management mirrors the MT4 EA: the strategy never pyramids and never holds both directions simultaneously.
- Comments inside the code are in English per project guidelines, while this README includes extended documentation for easier onboarding.

## Usage Tips

1. Assign the desired Forex pair to the strategy, leave the candle type at 5 minutes, and start the strategy. The indicators will warm up automatically.
2. Consider enabling a non-zero stop loss when running on live data—the original script recommended trading without it, but trailing stops alone may not be sufficient for risk control.
3. For algorithmic portfolios you can add this strategy to a `BasketStrategy` and manage capital allocation externally while still benefiting from the exposed parameters for optimization.

This documentation, along with the Russian and Chinese translations in the same folder, provides full transparency of the converted logic.
