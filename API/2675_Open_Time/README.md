# Open Time Strategy

## Overview
Open Time Strategy is a time-scheduled trading system that replicates the behaviour of the MetaTrader 5 expert advisor *OpenTime*. The strategy watches the market clock on finished candles and opens trades only inside a configurable time window. It can close any active position during a dedicated exit window, apply an optional trailing stop, and enforce basic stop-loss and take-profit rules expressed in pips.

Unlike the original hedging version, this StockSharp port works on a netted portfolio: when a signal appears that conflicts with the current position, the strategy first closes the opposite exposure and then opens the requested direction with the configured volume.

## Trade Workflow
1. **Closing window** – If the *Use Close Window* flag is enabled and the current time falls inside the close window, the strategy immediately exits any open position. No new trade is allowed until the window finishes.
2. **Trailing update** – When trailing is enabled and the market has moved at least `TrailingStop + TrailingStep` pips in favour of the current position, the trailing stop is pulled closer to price by the distance defined in `TrailingStop`. This recreates the MT5 logic where the stop level is modified only after a minimal step.
3. **Risk checks** – On every finished candle the strategy verifies whether stop-loss or take-profit thresholds have been touched. If any level is hit, the position is closed and all internal state for that side is reset.
4. **Entry window** – When the time is inside the trade window, the strategy evaluates the direction switches:
   - If long entries are enabled and the current net position is flat or short, it buys the configured volume plus any quantity required to cover an existing short position.
   - If short entries are enabled and the net position is flat or long, it sells the configured volume plus any quantity required to flatten an existing long position.

Each executed entry stores the entry price together with stop and target offsets (if different from zero). These values are reused by the trailing logic and the subsequent exit checks.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| Candle Type | 1 minute candles | Data type used for time tracking; the strategy reacts only on finished candles. |
| Use Close Window | true | Enables the automatic closing window. |
| Close Hour / Close Minute | 20:50 | Start of the closing window. Hour supports values 0–24; 24 rolls over to the next day. |
| Enable Trailing | false | Activates the trailing-stop logic. |
| Trailing Stop | 30 pips | Distance between price and the trailing stop. Converted to price units depending on the instrument’s tick size. |
| Trailing Step | 3 pips | Additional move required before the trailing stop is advanced again. |
| Trade Hour / Trade Minute | 18:50 | Start time of the trading window that allows new entries. |
| Duration | 300 seconds | Duration shared by both the opening and closing windows. |
| Enable Sell / Enable Buy | Sell = true, Buy = false | Selects which directions are allowed. |
| Volume | 0.1 | Order volume submitted with new entries. When reversing, extra volume is added to flatten the opposite exposure. |
| Stop Loss | 0 pips | Initial stop-loss distance. A value of zero disables the static stop and leaves exit control to trailing or the closing window. |
| Take Profit | 0 pips | Initial take-profit distance. A value of zero disables the profit target. |

## Implementation Details
- Pip values are recalculated from `Security.PriceStep`. For symbols quoted with three or five decimals the step is multiplied by ten to reproduce the original MT5 “pip” conversion.
- Both trailing and static risk levels operate on candle extremes (`HighPrice`/`LowPrice`) to approximate tick-by-tick behaviour while working in the candle-based high-level API.
- The strategy resets internal state after every exit to avoid reusing outdated stops or targets on the next trade.
- Because StockSharp works with net positions by default, simultaneous long and short positions are not supported. The reversal logic mimics MT5 hedging by offsetting the existing exposure before opening the requested side.

## Usage Notes
- Choose a candle type that matches the time granularity required by the trading window. A shorter timeframe (e.g., 1 minute) provides more precise timing.
- The closing and opening windows share the same duration parameter. To disable either window set the duration to zero or switch off *Use Close Window*.
- Trailing stops activate only when the market has advanced at least `Trailing Stop + Trailing Step` pips from the recorded entry price, reproducing the original trailing step behaviour.
