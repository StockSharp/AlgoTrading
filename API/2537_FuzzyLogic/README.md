# Fuzzy Logic Strategy

## Overview

The Fuzzy Logic strategy replicates the MT5 expert advisor **Fuzzy logic (barabashkakvn's edition)** using the high-level StockSharp API. The system measures trend strength and momentum exhaustion with Bill Williams oscillators and momentum indicators, converts those readings into fuzzy membership grades, and aggregates them into a single decision score between 0 and 1.

Trading actions are triggered when the fuzzy score crosses calibrated thresholds:

- **Decision &gt; 0.75** – open a short position (strong exhaustion / overbought conditions).
- **Decision &lt; 0.25** – open a long position (strong bullish reversal setup).

Positions are managed with fixed take-profit and stop-loss distances expressed in price steps. When a trailing stop distance is supplied, the protective stop is converted into a trailing one.

## Indicator Stack

| Component | Purpose |
| --- | --- |
| **Gator oscillator** (built from Alligator lines) | Measures the sum of jaw–teeth and teeth–lips spreads to gauge trend expansion or contraction. |
| **Williams %R (14)** | Detects overbought / oversold levels. |
| **Acceleration/Deceleration Oscillator (AC)** | Counts consecutive momentum shifts to estimate trend acceleration. |
| **DeMarker (14)** | Confirms exhaustion through high/low comparisons. Implemented directly inside the strategy. |
| **RSI (14)** | Tracks classic momentum swings. |

Alligator lines are calculated with smoothed moving averages and shifted forward exactly as in the original expert advisor to reproduce the Gator oscillator. AC values are derived from the Awesome Oscillator (5/34 SMA difference) minus its 5-period moving average, providing identical readings to MT5's `iAC` indicator.

## Trading Logic

1. Each indicator value is mapped to five fuzzy membership sets (very bearish → very bullish). Piecewise-linear functions replicate the original MT5 arrays.
2. The five membership groups are weighted (0.133, 0.133, 0.133, 0.268, 0.333) and aggregated into four summary bins.
3. The fuzzy decision score is computed as `Σ summary[x] * (0.2 * (x + 1) - 0.1)`, producing values in the `[0, 1]` range.
4. Signals are evaluated once per finished candle. The strategy stays flat unless the decision breaches the entry thresholds.
5. Order size relies on the `Volume` property (default 1). Protective stops are registered through `StartProtection`.

## Risk Management

- **StopLossPoints** – absolute distance (in price steps) for the protective stop. Used when `TrailingStopPoints` is zero.
- **TrailingStopPoints** – if &gt; 0, the stop-loss distance switches to this value and trailing mode is enabled.
- **TakeProfitPoints** – absolute distance for the profit target.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Time frame / candle type used for calculations. |
| `BuyThreshold` | Fuzzy score below which a long entry is opened. Default 0.25. |
| `SellThreshold` | Fuzzy score above which a short entry is opened. Default 0.75. |
| `StopLossPoints` | Stop-loss distance in instrument price steps. Default 60. |
| `TakeProfitPoints` | Take-profit distance in price steps. Default 20. |
| `TrailingStopPoints` | Trailing stop distance in price steps. Default 0 (disabled). |
| `WilliamsPeriod` | Lookback for Williams %R. Default 14. |
| `RsiPeriod` | Lookback for RSI. Default 14. |
| `DeMarkerPeriod` | Lookback for the embedded DeMarker calculation. Default 14. |

## Implementation Notes

- The DeMarker oscillator is implemented manually because StockSharp does not expose a built-in version. High and low deltas are queued to reproduce MT5 sums.
- AC history stores the five most recent completed values so the fuzzy logic can check consecutive acceleration streaks just like `iAC(..., shift)` in MT5.
- Alligator jaw/teeth/lips buffers introduce the same forward shift (8/5/3 bars) before deriving the Gator histogram values.
- The strategy only opens a new position when `Position == 0`, respecting the single-position behavior of the original expert advisor.

## Usage Steps

1. Attach the strategy to a portfolio and security in Designer/Backtester.
2. Configure the desired candle series via `CandleType`.
3. Adjust thresholds or stop distances if needed.
4. Start the strategy; it will trade automatically when the fuzzy score crosses the configured levels.
