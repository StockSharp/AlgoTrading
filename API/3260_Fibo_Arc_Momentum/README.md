# Fibo Arc Momentum Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader expert advisor "FiboArc" (folder `MQL/24924`). The original EA combines
multiple momentum filters with Fibonacci arc breakouts. The StockSharp implementation keeps the same idea while adapting it to
the high-level candle API:

* Two linear weighted moving averages (`FastMaPeriod`, `SlowMaPeriod`) define the trend direction.
* A momentum oscillator measured against the neutral 100 level filters out weak setups.
* A MACD histogram confirms trend strength and detects fresh crossovers.
* A simplified Fibonacci arc is rebuilt every bar using the open prices of two anchor candles selected by
  `TrendAnchorLength` and `ArcAnchorLength`. A breakout through this dynamic level replaces the object-based checks from the
  MetaTrader version.

The strategy works on any symbol/timeframe pair supported by StockSharp. All calculations run on fully finished candles to
mirror the EA behaviour and avoid lookahead bias.

## Indicators and data flow

The strategy subscribes to a single candle stream configured by `CandleType`. Each new finished candle is fed into the
following indicators via `SubscribeCandles(...).BindEx(...)`:

| Indicator | Purpose | Default settings |
|-----------|---------|------------------|
| LinearWeightedMovingAverage (fast) | Short-term trend and entry timing | `FastMaPeriod = 6`, typical price |
| LinearWeightedMovingAverage (slow) | Higher level trend filter | `SlowMaPeriod = 85`, typical price |
| Momentum | Distance from 100 is used to confirm strong moves | `MomentumPeriod = 14` |
| MovingAverageConvergenceDivergenceSignal | Confirms the trend and spots crossovers | `MacdFastPeriod = 12`, `MacdSlowPeriod = 26`, `MacdSignalPeriod = 9` |

Indicator outputs are received as `IIndicatorValue` instances; only final values are processed. No manual `GetValue()` calls are
required, which keeps the implementation compliant with repository guidelines.

## Fibonacci arc reconstruction

MetaTrader draws an actual arc object and reads its values with `ObjectGetValueByShift`. StockSharp does not rely on chart
objects, so the arc is emulated numerically:

1. The strategy keeps a rolling list of finished candles (`_history`).
2. `TrendAnchorLength` selects the index of the base anchor, and `ArcAnchorLength` selects the second anchor.
3. The arc level for the current candle is computed as a linear interpolation between the anchor opens using
   `FibonacciRatio` (default 0.618).
4. For breakout detection we compare the previous candle open with the previous arc level and the current candle open with the
   newly calculated level. A cross from below (`fibCrossUp`) or from above (`fibCrossDown`) recreates the original EA checks.

This approach keeps the trading logic intact while avoiding graphical objects.

## Trading rules

### Long entries

A long position is opened when all conditions below are satisfied:

1. The previous bar opened below the previous arc level and the current bar opens above the new level (`fibCrossUp`).
2. The fast LWMA is above the slow LWMA (`bullishTrend`).
3. The absolute distance between momentum and 100 is at least `MomentumThreshold`.
4. The MACD main line is above its signal line, or it has just crossed above (`macdAboveSignal` or `macdCrossUp`).
5. The current position size is less than or equal to zero (no existing long exposure).

The strategy buys `Volume` plus the absolute value of any open short exposure to ensure flat-to-long transitions.

### Short entries

Short trades mirror the long logic:

1. `fibCrossDown` confirms a breakout to the downside.
2. The fast LWMA is below the slow LWMA.
3. Momentum distance exceeds `MomentumThreshold`.
4. MACD is below its signal line or crosses down.
5. No existing long exposure remains.

### Exits

Positions are closed when one of the following occurs:

* Trend or MACD conditions flip against the trade.
* The opposite Fibonacci breakout signal appears.
* The adaptive stop-loss or take-profit level is touched.

All exits are executed with market orders to stay consistent with the MetaTrader version that used immediate closes and trailing
logic.

## Risk management

The original EA offered money-based stops, trailing logic, and break-even protection. The StockSharp strategy keeps the same
features with transparent parameters:

* `StopLossDistance` and `TakeProfitDistance` define fixed distances in price units from the filled price.
* `EnableBreakEven`, `BreakEvenTrigger`, and `BreakEvenOffset` control the move-to-break-even behaviour.
* `EnableTrailing`, `TrailingTrigger`, and `TrailingDistance` implement a candle-based trailing stop.

The current stop, target, and entry price are tracked internally and updated on every candle. When a filled trade is reported via
`OnNewMyTrade`, the stop and target levels are recalculated from the actual execution price.

## Parameters

| Name | Description |
|------|-------------|
| `CandleType` | Timeframe (and aggregation type) used for all calculations. |
| `FastMaPeriod`, `SlowMaPeriod` | Trend-defining LWMA lengths. |
| `MomentumPeriod`, `MomentumThreshold` | Momentum filter settings. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD configuration. |
| `TrendAnchorLength`, `ArcAnchorLength`, `FibonacciRatio` | Fibonacci arc reconstruction controls. |
| `StopLossDistance`, `TakeProfitDistance` | Initial stop and target distances (absolute price units). |
| `EnableBreakEven`, `BreakEvenTrigger`, `BreakEvenOffset` | Break-even logic. |
| `EnableTrailing`, `TrailingTrigger`, `TrailingDistance` | Trailing stop configuration. |

All parameters are exposed through `StrategyParam<T>` and support optimization where it makes sense. Default values follow the
original EA.

## Usage

1. Attach the strategy to a security and set `Volume` according to the desired position size.
2. Optionally adjust the timeframe, moving-average lengths, and Fibonacci settings to match the target market.
3. Launch the strategy. All decisions rely on finished candles; intrabar execution is not required.
4. Review the built-in charting helpers for the fast/slow LWMA and MACD panels if the host supports visualization.

The strategy is self-contained and does not modify the shared test suite. No Python version is provided per the task
requirements.
