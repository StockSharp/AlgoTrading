# Hans123 Trader Strategy

## Overview
Hans123 Trader is a breakout system converted from the original MetaTrader 5 expert advisor *Hans123_Trader*. The strategy scans a rolling price range and places pending stop orders during a configurable intraday window. Protective stops, profit targets, and trailing rules mirror the MQL5 logic so the StockSharp port behaves like the source robot.

## Core Concepts
- **Range breakout** – uses the highest high and lowest low of the last *N* candles to define the breakout channel.
- **Time filter** – only evaluates signals between the start and end hours to avoid overnight noise.
- **Synchronous pending orders** – refreshes buy stop and sell stop orders every completed candle inside the trading window.
- **Risk control** – optional stop-loss, take-profit, and trailing stop distances expressed in pips.
- **Dynamic trailing** – once price travels the trailing stop plus trailing step distance, the protective stop is tightened to lock in gains.

## Trading Logic
1. Subscribe to the selected candle series and wait for the `RangeLength` indicator window to form.
2. On each finished candle:
   - Update the 80-bar (configurable) high/low channel.
   - Skip processing if the current time lies outside the `[StartHour, EndHour)` interval.
   - Cancel any existing entry orders and place fresh stop orders:
     - **Buy stop** at the range high for `OrderVolume`.
     - **Sell stop** at the range low for `OrderVolume`.
3. When an entry order fills:
   - Cancel the opposite pending order.
   - Register stop-loss and take-profit orders if the corresponding pip distances are greater than zero.
4. While a position is open:
   - If the price advances by at least `TrailingStopPips + TrailingStepPips`, move the protective stop toward the market by `TrailingStopPips`.
   - Protective orders are canceled automatically when the position returns to flat.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Order size for breakout entries. | `0.1` |
| `RangeLength` | Number of candles in the breakout channel. | `80` |
| `StopLossPips` | Stop-loss distance in pips (0 disables the stop). | `50` |
| `TakeProfitPips` | Take-profit distance in pips (0 disables the target). | `50` |
| `TrailingStopPips` | Trailing stop distance in pips (0 disables trailing). | `10` |
| `TrailingStepPips` | Additional pips required before the trailing stop updates. Must be positive when trailing is enabled. | `5` |
| `StartHour` | Inclusive hour of day (UTC) when breakout orders start. | `6` |
| `EndHour` | Exclusive hour of day (UTC) when breakout orders stop. | `10` |
| `CandleType` | Working candle data type and timeframe. | `1 hour` candles |

## Practical Notes
- The pip size adapts to the security decimals (3/5 digit forex symbols receive the usual *×10* adjustment).
- Trailing stops are only created after a position travels the activation distance; if `StopLossPips` is zero the initial stop is omitted until trailing conditions are met.
- Keep portfolio permissions aligned with the selected `OrderVolume` and instrument contract size.
- The StockSharp conversion uses chart helpers to visualize candles, the channel, and trades for debugging.

## Differences vs. MQL5 Version
- Stop and target orders are registered through StockSharp high-level helpers instead of MetaTrader trade requests.
- Volume defaults remain identical (0.1 lots) but can be optimized via `StrategyParam` metadata.
- Pending orders are refreshed on every completed candle instead of waiting for tick-level updates, matching StockSharp's event model.

## Usage
1. Attach the strategy to a portfolio/security pair and verify the candle subscription matches your desired timeframe.
2. Adjust parameters for the instrument volatility and session boundaries.
3. Start the strategy; monitor the chart area overlay to confirm breakout levels and executed trades.
4. Use the built-in parameters for optimization inside the StockSharp testing environment if desired.
