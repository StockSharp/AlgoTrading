# DreamBot Strategy

## Overview
DreamBot is a StockSharp port of the MetaTrader 4 expert advisor "DreamBot". The strategy monitors the Force Index oscillator on hourly candles and waits for the momentum to cross bullish or bearish thresholds. When the Force Index crosses above the bullish level after being below it on the previous bar, the strategy opens a long position. When the Force Index crosses below the bearish level after being above it, the strategy opens a short position. Trading occurs only when there is no existing position, mirroring the single-position logic of the original robot.

## Trading logic
- Subscribe to H1 candles and compute a smoothed Force Index (length 13 by default).
- Track the last two completed Force Index values. Signals are generated using the *previous* bar values, exactly like the MT4 implementation (`iForce` with shift 1 and 2).
- Enter long when the Force Index on the previous candle is above `BullsThreshold` and the value two candles back was below the threshold, provided no position is open.
- Enter short when the Force Index on the previous candle is below `BearsThreshold` and the value two candles back was above the threshold, provided no position is open.
- Optional trailing stop replicates the original EA: once profit exceeds `TrailingStepPoints`, a stop level is pulled to `TrailingStartPoints` away from price and follows further advances.

## Risk management
- `StartProtection` attaches classic stop-loss and take-profit orders using the MetaTrader "points" distance converted through the instrument price step.
- Trailing protection is market-based: when the computed trailing level is breached, the strategy sends a market order to close the position immediately.
- Position tracking captures the volume-weighted entry price so the trailing logic aligns with partial fills and reversals.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `ForcePeriod` | Force Index smoothing period (default 13). |
| `TakeProfitPoints` | Take-profit distance in MetaTrader points. |
| `StopLossPoints` | Stop-loss distance in MetaTrader points. |
| `BullsThreshold` | Bullish Force Index threshold that enables long entries. |
| `BearsThreshold` | Bearish Force Index threshold that enables short entries. |
| `EnableTrailing` | Enables the trailing stop logic. |
| `TrailingStartPoints` | Distance (in points) maintained between price and trailing stop once activated. |
| `TrailingStepPoints` | Profit (in points) required before the trailing stop activates. |
| `CandleType` | Timeframe used for Force Index calculations (defaults to H1 candles). |

## Notes
- The parameter validation keeps the trailing trigger (`TrailingStepPoints`) from exceeding the trailing distance (`TrailingStartPoints`), matching the MetaTrader safety check.
- Stop-level enforcement from the original EA (broker `MODE_STOPLEVEL`) is approximated through StockSharp's price-step conversions. Depending on broker constraints, additional validation may be required.
- All code comments and logs are provided in English as requested by the conversion guidelines.
