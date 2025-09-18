# VarMovAvg Strategy

## Overview
The VarMovAvg strategy is a stop-and-reverse system converted from the MetaTrader 4 expert advisor `VarMovAvg_v0011`. It uses an adaptive Variable Moving Average (VMA) to measure trend direction and waits for a two-step pullback pattern (called Bar A and Bar B in the original EA) before reversing the position. While a position is active, a moving-average-based trailing stop protects profits and flips the trade when the opposite Bar A/Bar B sequence completes.

## Trading Logic
1. **Adaptive VMA** – The custom `VariableMovingAverage` indicator replicates the MT4 formula:
   - Efficiency ratio compares the current close with the close `AmaPeriod` bars ago and divides it by the accumulated absolute price movement.
   - The smoothing coefficient interpolates between the fast and slow periods and is raised to the `SmoothingPower` parameter just like the original `G` value.
2. **Signal Detection (Bar A / Bar B)** – Two independent state machines track long and short setups:
   - *Bar A*: Price moves `SignalPipsBarA` (in pips) beyond the VMA in the potential trade direction.
   - *Bar B*: Price extends another `SignalPipsBarB` pips in the same direction, locking the extreme price.
   - *Entry*: When the close returns to the entry band defined by `SignalPipsTrade ± EntryPipsDiff`, the strategy enters (or reverses) using market orders.
3. **Trailing Stop and Reversal** – While a position is open, a moving average computed on highs (for shorts) or lows (for longs) is shifted by `StopMaShift` bars and padded by `StopPipsDiff`.
   - If the candle pierces the stop level, the position is closed.
   - If the opposite Bar A/Bar B sequence triggers while a position exists, the strategy issues a single market order sized as `|Position| + Volume` to flip direction immediately, matching the EA behaviour.

## Parameters
| Parameter | Description | MT4 Source |
|-----------|-------------|------------|
| `AmaPeriod` | Lookback window used by the VMA. | `prm.vma.periodAMA` |
| `FastPeriod` | Fast smoothing factor inside the VMA. | `prm.vma.nfast` |
| `SlowPeriod` | Slow smoothing factor inside the VMA. | `prm.vma.nslow` |
| `SmoothingPower` | Exponent `G` applied to the adaptive coefficient. | `prm.vma.G` |
| `SignalPipsBarA` | Distance from the VMA required to accept Bar A. | `prm.sig.pipsBarA` |
| `SignalPipsBarB` | Additional distance required to accept Bar B. | `prm.sig.pipsBarB` |
| `SignalPipsTrade` | Offset from the Bar B extreme to the entry line. | `prm.sig.pipsTrade` |
| `EntryPipsDiff` | Accepted tolerance around the entry line. | `prm.entry.diff` |
| `StopPipsDiff` | Offset applied to the trailing stop moving average. | `prm.stop.diff` |
| `StopMaPeriod` | Period of the stop moving average. | `prm.mastop.period` |
| `StopMaShift` | Shift (bars) of the stop moving average. | `prm.mastop.shift` |
| `StopMaMethod` | Moving average method (`MODE_SMA`, `EMA`, `SMMA`, `LWMA`). | `prm.mastop.method` |
| `CandleType` | Working timeframe. | Chart timeframe |

> **Pip conversion** – All pip distances are multiplied by `Security.PriceStep` when it is available. If the instrument does not have a configured step, the raw values are interpreted in price units, replicating the EA fallback.

## Usage Notes
- The strategy relies on `SubscribeCandles` and runs entirely on finished candles; the entry band logic mirrors the EA’s tick-by-tick checks using close prices.
- Protective orders are modelled through market exits when the candle crosses the stop level, which matches the EA behaviour because stop orders were recalculated every tick.
- The moving-average shift is implemented via a FIFO buffer, ensuring `StopMaShift = 0` uses the latest value and positive shifts look back the requested number of bars.
- After every trade (entry, reversal, or stop hit) both signal trackers reset to the neutral state to avoid duplicate orders, emulating the `STATUS_TRADE` reset logic in MetaTrader.

## Quick Start
1. Add the strategy to a StockSharp environment and assign an instrument with a valid `PriceStep` and tick size.
2. Configure the timeframe through `CandleType` (the original expert was tested on intraday charts such as M5).
3. Adjust the pip distances and trailing parameters to match the broker’s quote precision.
4. Start the strategy; it will alternate between long and short positions whenever the Bar A/Bar B conditions are met.

## Differences from the Original EA
- The StockSharp version works on closed candles instead of tick-by-tick execution. The entry tolerance band keeps the trigger timing close to the MT4 behaviour.
- Stop-loss handling is implemented by checking candle extremes rather than placing/modifying MT4 orders, because StockSharp strategies typically manage exits programmatically.
- The `VariableMovingAverage` indicator is implemented directly in C# and exposes the smoothing power, eliminating the unused `dK` parameter that existed in the MQL source.
