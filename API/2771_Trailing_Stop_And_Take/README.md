# Trailing Stop And Take Strategy

## Overview
The **Trailing Stop And Take Strategy** is a direct StockSharp adaptation of the MetaTrader expert advisor from `MQL/19963`. It focuses on active trade management: once a position is open, the strategy attaches initial stop-loss and take-profit levels and then trails both levels as price moves. Trailing adjustments respect configurable minimum step sizes, breakeven protection, and the option to avoid trailing while a trade is still losing.

The strategy operates on a single security using finished candles. When the strategy is flat it opens a position in the direction of the most recent candle body (bullish closes lead to longs, bearish closes lead to shorts). This mirrors the original test behavior used by the MQL script and provides a continuous flow of positions for the trailing engine to manage.

## How It Works
1. Subscribe to the configured candle type and process only finished candles.
2. When no position is open, enter long on bullish candles or short on bearish candles (respecting the position type filter).
3. On a new position, initialize stop-loss and take-profit distances using `InitialStopLossPoints`/`InitialTakeProfitPoints`. If those are zero, the trailing distances are used instead.
4. On each candle close, compute updated trailing targets:
   - Stops move closer to price only after the market advances by the trailing step.
   - Take profits move closer when price retraces by at least the trailing step.
   - Breakeven protection prevents moving levels into a loss zone when `AllowTrailingLoss` is disabled.
5. When price crosses a trailing stop or take-profit level, exit via market order and reset all stored levels.

## Trailing Logic
### Long Positions
- Initial stop is clamped to at least `SpreadMultiplier * PriceStep` away from entry.
- Initial take profit is positioned at least the same minimum distance above entry.
- Trailing stop follows the close price downward by `TrailingStopLossPoints` while respecting the trailing step and optional breakeven filter.
- Trailing take profit tightens when price retraces, never moving below the breakeven level when trailing losses are disallowed.

### Short Positions
- Initial stop is set above entry, no closer than the spread multiplier distance.
- Initial take profit starts below entry with the same minimum distance rule.
- Trailing stop lowers as price falls, but will not move higher than breakeven unless loss trailing is permitted.
- Trailing take profit rises toward price on retracements, clamped to breakeven when needed.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle aggregation used for price evaluation. |
| `Volume` | Default order volume for entries and exits. |
| `PositionType` | Restricts the engine to manage long positions, short positions, or both. |
| `InitialStopLossPoints` | Initial stop-loss size in price points (uses trailing distance if zero). |
| `InitialTakeProfitPoints` | Initial take-profit size in price points (uses trailing distance if zero). |
| `TrailingStopLossPoints` | Distance between price and trailing stop. |
| `TrailingTakeProfitPoints` | Distance between price and trailing take profit. |
| `TrailingStepPoints` | Minimum movement in points required before adjusting stops or targets. |
| `AllowTrailingLoss` | Enables trailing while the trade is still below breakeven. |
| `BreakevenPoints` | Offset in points added to the entry price to form the breakeven barrier. |
| `SpreadMultiplier` | Multiplier for the minimal stop distance approximation (simulates the MQL `StopLevel`). |

## Notes
- Stops and targets are executed with market orders when triggered, which keeps the implementation simple and mirrors the original stop modifications.
- `SpreadMultiplier` approximates the MQL behavior where stop levels cannot be placed closer than the current spread. Adjust this value to match the execution venue.
- The strategy intentionally avoids a Python version and focuses solely on the C# implementation, as requested.
- Consider combining the trailing engine with your own entry filter by disabling the built-in entries and injecting external orders if required.
