# CCI and Martin Strategy

## Overview
The CCI and Martin strategy searches for sharp reversals after a short bearish or bullish sequence and confirms the move with the Commodity Channel Index. The logic replicates the original MetaTrader 5 expert advisor while using StockSharp's high-level API. The strategy works on finished candles only and can operate on any instrument for which CCI values and price steps are available.

## Trading Rules
- **Bullish setup**
  - Candle `-2` and candle `-1` must both be bearish (open greater than close).
  - Candle `0` must close above its open and above the open of candle `-1`.
  - CCI on candle `-1` must be below `+5`, below the value of candle `-2`, and both `-2` and `-3` must show a decreasing sequence. The current CCI (candle `0`) must turn upward above the previous value.
  - When all conditions hold and no position is open, the strategy enters a long trade.
- **Bearish setup**
  - Candle `-2` and candle `-1` must both be bullish (open less than close).
  - Candle `0` must close below its open and below the open of candle `-1`.
  - CCI on candle `-1` must be above `-5`, above the value of candle `-2`, and both `-2` and `-3` must form an increasing sequence. The current CCI (candle `0`) must turn downward below the previous value.
  - When all conditions hold and no position is open, the strategy enters a short trade.

The algorithm monitors only completed candles. The original MQL implementation waited 40 seconds after the minute open to avoid premature signals; using finished candles makes this filter unnecessary.

## Risk Management
- **Stop-loss** and **take-profit** distances are defined in pips. They are converted to price offsets by multiplying the instrument's price step by ten when the step corresponds to a 3- or 5-digit quote, mirroring the original pip calculation.
- **Trailing stop** becomes active after the price advances by the trailing stop distance plus the trailing step. The stop is then moved to maintain the trailing distance and only advances when price improvement exceeds the configured step.
- If stop-loss or take-profit is set to zero the respective exit is disabled. Trailing requires both stop distance and step to be positive.

## Volume Management
Two optional position-sizing engines can alter the lot size after each trade.
- **Martingale scaling** multiplies the current volume by the martingale coefficient once the number of consecutive losses reaches the trigger. Scaling stops after the configured number of martingale steps. Any profitable trade resets the volume to the initial value.
- **Step adjustments** increment the volume by a fixed amount either after losses or after profits, depending on the selected mode. The increment is normalised to the instrument's volume step and capped by the maximum volume parameter. When the limit is exceeded or a trade does not meet the trigger condition, the volume falls back to the initial size.

The original expert advisor forbids enabling martingale and step logic simultaneously; the C# port enforces the same restriction.

## Parameters
- `CandleType` – candle series used for analysis.
- `CciPeriod` – averaging length for the Commodity Channel Index.
- `InitialVolume` – base order size before any scaling.
- `StopLossPips` – stop-loss distance expressed in pips.
- `TakeProfitPips` – take-profit distance expressed in pips.
- `TrailingStopPips` – trailing stop distance in pips (0 disables trailing).
- `TrailingStepPips` – minimum price improvement required before the trailing stop moves.
- `EnableMartingale` – activates martingale-style scaling after losses.
- `MartingaleCoefficient` – multiplier applied to the current volume for martingale trades.
- `MartingaleTriggerLosses` – number of consecutive losing trades needed before scaling.
- `MartingaleMaxSteps` – maximum number of martingale multiplications.
- `EnableStepAdjustments` – enables step-based volume increments.
- `StepVolumeIncrement` – absolute increment applied when the step rule triggers.
- `StepVolumeMax` – upper bound for the step-based volume.
- `StepAdjustmentMode` – selects whether the step increment fires after a loss or after a profit.

## Notes
- The strategy assumes market orders fill close to the requested price. Protective logic recalculates stops on every finished candle to emulate the tick-based trailing in the original EA.
- If the instrument's price step does not correspond to classic FX quoting the pip conversion still works, but pip-based distances may represent different monetary values.
