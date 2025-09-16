# ExFractals Strategy

## Overview

The ExFractals strategy is a breakout system that combines Williams-style fractal levels with the ExVol average body momentum filter. The algorithm continuously monitors the most recent confirmed fractal highs and lows, averages them in pairs, and opens trades when price closes beyond those averaged levels while the ExVol reading confirms the direction of the move.

## Trading Logic

1. **Fractal detection**
   - Candles are processed once they close.
   - Upward (bearish) and downward (bullish) fractals are detected once the middle candle in a five-candle window is a strict extreme compared with its neighbors.
   - The strategy stores the two latest confirmed fractals per side together with their timestamps.
   - Each side produces an actionable level equal to the average of the last two fractal prices. Duplicate timestamps are ignored to prevent using the same fractal twice.
2. **ExVol filter**
   - The ExVol value equals the simple average of the candle body (close minus open) expressed in price steps during the selected lookback period.
   - A negative ExVol indicates persistent bullish candles (positive close-to-open), and a positive ExVol indicates persistent bearish candles.
3. **Entry conditions**
   - **Long:** the last close is above the averaged upper fractal level and ExVol is negative. Any active short position is closed and a new long position is opened.
   - **Short:** the last close is below the averaged lower fractal level and ExVol is positive. Any active long position is closed and a new short position is opened.
4. **Risk and exit rules**
   - Fixed stop-loss and take-profit targets are placed at configurable pip distances from the entry price.
   - Optional trailing stops move only after the trade gains at least `trailing stop + trailing step` pips. The stop is pulled up/down to maintain a constant trailing distance while respecting the minimum trailing step.
   - If price hits the stop-loss or take-profit the whole position is closed.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `Candle Type` | Candle data type/time-frame used by the strategy. | 1 hour time-frame |
| `ExVol Period` | Number of closed candles used to average the candle body (ExVol). | 15 |
| `Stop Loss` | Stop-loss distance in pips from the entry price. Set to `0` to disable. | 40 |
| `Take Profit` | Take-profit distance in pips from the entry price. Set to `0` to disable. | 100 |
| `Trailing Stop` | Trailing stop distance in pips. Set to `0` to disable trailing. | 30 |
| `Trailing Step` | Additional price movement (in pips) required before moving the trailing stop. Must be positive when trailing is enabled. | 5 |
| `Volume` | Default order volume inherited from the base `Strategy` class. | 1 |

## Additional Notes

- The trailing logic mirrors the MetaTrader implementation: the stop is not adjusted until the position is at least `TrailingStop + TrailingStep` pips in profit.
- ExVol calculations rely on the instrument `PriceStep`; if the step is not available a default value of 0.0001 is used.
- The strategy issues market orders via `BuyMarket` and `SellMarket`, automatically reversing any existing position before opening a new one.
- Ensure that your data feed provides enough historical candles to form the initial fractal pairs (at least five closed candles).
