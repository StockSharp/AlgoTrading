## Overview
The **Hans123Trader Strategy** recreates the MetaTrader expert advisor "Hans123Trader v1" using the StockSharp high-level API. The system arms stop orders twice per day based on the break of the most recent 5-minute trading range. It is tailored for Forex-style symbols where price steps correspond to fractional pips. Pending orders are refreshed each trading day and any open position is forcefully closed when the calendar rolls over.

## Core Workflow
1. **Range tracking** – a rolling 80-bar window of 5-minute candles is maintained via `Highest` and `Lowest` indicators. The most recent high and low define the breakout levels.
2. **Session scheduling** – two independent trading windows are controlled by `EndSession1` and `EndSession2`. When the clock reaches the configured hour (minute `00`), the strategy calculates new pending stop orders.
3. **Order placement** – a buy stop is submitted `5` points above the detected high and a sell stop `5` points below the detected low. Orders are removed the moment a new day starts to mimic the MetaTrader expiration at 23:59.
4. **Position management** – after entry the strategy applies the requested initial stop-loss, optional take-profit and trailing stop. Protective levels are expressed in points and converted into price using the instrument's `PriceStep`.
5. **Daily hygiene** – if a position remains open when a new trading day begins, it is closed at market. All pending orders from the previous day are cancelled before new ones are prepared.

## Trading Rules
- **Entry signals**
  - Two breakout attempts per day: one at `EndSession1`, another at `EndSession2` (hours are broker/server time).
  - Buy stop price = `HighestHigh + 5 points`. Sell stop price = `LowestLow − 5 points`.
  - Both orders use the current `Volume` parameter (default `1`).
  - Orders are skipped if volume is non-positive.
- **Exit logic**
  - Initial stop-loss = entry price ± `InitialStopLoss` points (below for longs, above for shorts).
  - Take-profit = entry price ± `TakeProfit` points (above for longs, below for shorts).
  - Trailing stop tightens the protective level whenever the close moves further into profit by at least `TrailingStop` points.
  - Any position that survives to the next day is closed immediately at market.
- **Order maintenance**
  - Pending stop orders are cancelled at the beginning of every calendar day.
  - Once a stop order is triggered (or cancelled/failed), internal references are cleared automatically.

## Parameters
| Name | Description |
| --- | --- |
| `BeginSession1` / `BeginSession2` | Preserved for UI compatibility (start-hour hints). The current implementation relies on the end-hour triggers. |
| `EndSession1` / `EndSession2` | Hours (0–23) when new stop orders are armed; the minutes must be exactly zero. |
| `TrailingStop` | Trailing distance in points. `0` disables trailing. |
| `TakeProfit` | Take-profit distance in points. `0` disables take-profit. |
| `InitialStopLoss` | Initial stop-loss distance in points. `0` leaves the trade without a protective stop unless trailing activates. |
| `CandleType` | Candle series used for the 80-bar range (default `TimeSpan.FromMinutes(5)`). |
| `Volume` | Strategy base volume inherited from `Strategy`. |

## Conversion Notes
- The MetaTrader helper functions `OrderSendExtended` and the global-variable lock are not required; StockSharp manages concurrency internally.
- Magic numbers are replaced by explicit order references (`_session*` fields). Order lifecycle events clear these references when the order finishes.
- Pending orders expiring at 23:59 are emulated by cancelling them when a new day begins.
- Trailing stop logic uses candle close prices as a stand-in for the MetaTrader bid/ask quotes.
- All point-based distances are multiplied by `Security.PriceStep`. When `PriceStep` is not set the raw point values are treated as absolute price distances.

## Usage Tips
- Assign instruments with properly configured `PriceStep`, `StepPrice`, and `VolumeStep` so that point-to-price conversion and volume rounding are accurate.
- Verify that historical 5-minute data is available; the breakout levels depend on the most recent 80 candles.
- Adjust `EndSession1`/`EndSession2` to match the desired market sessions (e.g., pre-London and pre-New York breaks).
- Use Designer or Runner to optimise `InitialStopLoss`, `TakeProfit`, and `TrailingStop` for the chosen instrument before live deployment.
- Combine the strategy with StockSharp risk controls if multiple strategies share the same portfolio.
