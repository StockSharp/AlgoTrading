# Breakdown Catcher Strategy

## Overview
The Breakdown Catcher strategy is a bar-by-bar breakout system ported from the MetaTrader expert advisor "Breakdown catcher". After each completed candle the strategy places virtual breakout levels above the previous high and below the previous low (optionally shifted by an indent). When the next candle pierces one of these levels the strategy enters a position in the breakout direction and immediately assigns stop-loss, take-profit and optional trailing protection expressed in pips.

## Trading Logic
1. At the close of every candle the high and low of the completed bar become the reference range for the next period.
2. Buy breakout level = previous high + indent (in pips). Sell breakout level = previous low − indent.
3. If the current candle trades through the buy level while no position is open, the strategy opens a long position at market, removes any short context and stores protective levels.
4. If the current candle trades through the sell level while flat, the strategy opens a short position at market.
5. Stop-loss and take-profit distances are converted from pips to absolute prices by using the instrument price step and the classic MetaTrader adjustment for 3/5 decimal instruments.
6. A trailing stop can tighten the protective price after the trade moves in favour by at least `TrailingStop + TrailingStep` pips. The trailing step mimics the MetaTrader logic where the stop moves only after a sufficient additional move.
7. If both breakout levels are reached within the same candle the strategy skips trading for that bar to avoid ambiguous execution order.
8. A spread filter blocks new entries whenever the current bid-ask spread exceeds the configured `AllowedSpreadPoints`.

## Money Management
* The strategy uses the base `Strategy.Volume` for order size. When reversing positions, the volume is increased by the absolute value of the current position to ensure a full flip.
* Stop-loss, take-profit and trailing stops are handled internally by issuing market exit orders when price ranges include the protective levels.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `StopLossPips` | Stop-loss distance in pips. | `30` |
| `TakeProfitPips` | Take-profit distance in pips. | `90` |
| `TrailingStopPips` | Trailing stop distance in pips. Set to `0` to disable trailing. | `30` |
| `TrailingStepPips` | Additional progress required before the trailing stop moves. Must be positive when trailing is enabled. | `5` |
| `IndentPips` | Extra offset applied to breakout levels. | `0` |
| `AllowedSpreadPoints` | Maximum spread measured in raw points (`PriceStep` units). | `5` |
| `CandleType` | Candle series used for breakout detection. | `1h time frame` |

## Notes and Limitations
* The conversion from pips follows the same digit adjustment as the original EA: if the instrument has 3 or 5 decimals a pip equals ten price steps.
* Because the StockSharp high-level API works with candle events, the exact order in which both breakout levels are hit inside a single candle cannot be determined; therefore the strategy skips such bars.
* Protective orders are modelled with market exits, ensuring the strategy is self-contained without relying on broker-side stop orders.
