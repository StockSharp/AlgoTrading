# E-Friday Strategy

## Overview
- Converts the original MetaTrader expert advisor `E-Friday.mq5` to the StockSharp high-level API.
- Trades only when the chart time frame is **H1 or lower**; otherwise the strategy logs a warning and stays flat.
- Enters positions in a contrarian fashion: a bearish candle opens a long position and a bullish candle opens a short position.
- Completely disables trading every Friday to match the original weekend-protection behaviour.
- Restricts trading to a configurable time window and can force positions to be closed after the session end.

## Trading Logic
1. At each finished candle the strategy checks the current exchange time:
   - if the day is Friday it skips any action;
   - if the hour is before the configured start hour it waits;
   - if the closing window is enabled and the hour is above the end hour it flattens every position and skips new entries.
2. When trading is allowed the last completed candle drives the signal:
   - if `Open > Close` (bearish body) the strategy prepares a long entry;
   - if `Open < Close` (bullish body) the strategy prepares a short entry;
   - equal open and close prices cancel every pending action.
3. Before entering a new position the current exposure is flattened, so there is never more than one net position.

## Position Management
- **Lot size** – taken from `TradeVolume` and sent to `BuyMarket` / `SellMarket` orders.
- **Stop loss & take profit** – measured in pips. Pips are calculated from `Security.PriceStep` and multiplied by `10` when the instrument has 3 or 5 decimal places, exactly as in the MQL version.
- **Trailing stop** – activates once price moves by `TrailingStopPips + TrailingStepPips` in favour of the position. The stop is tightened to `current price - trailing stop` (long) or `current price + trailing stop` (short).
- Exits are evaluated using candle extrema:
  - a long position closes if the candle low touches the stop or the high reaches the take profit;
  - a short position closes if the candle high touches the stop or the low reaches the take profit.
- After the session end hour (when `UseCloseHour = true`) every open position is closed via market orders.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Time frame of the processed candles. Must define a positive `TimeSpan` and should not exceed one hour. |
| `TradeVolume` | Order volume in lots. Must be positive. |
| `StopLossPips` | Distance from the entry price to the protective stop, expressed in pips. Set to zero to disable the initial stop. |
| `TakeProfitPips` | Distance from the entry price to the profit target in pips. Set to zero to disable the target. |
| `TrailingStopPips` | Trailing stop distance in pips. Works together with `TrailingStepPips`. |
| `TrailingStepPips` | Minimal additional progress (in pips) required before the trailing stop is tightened. Must be positive when the trailing stop is enabled. |
| `StartHour` | Hour (exchange time) when the strategy may start opening positions. |
| `UseCloseHour` | Enables or disables forced closing after the end hour. |
| `EndHour` | Hour (exchange time) after which the strategy stops trading and closes existing positions. |

## Implementation Notes
- Uses `SubscribeCandles` and the high-level `Bind` API so that indicators can be added later if necessary.
- Validates the trailing configuration at start-up: when a trailing stop is requested the trailing step must be strictly positive.
- Pip conversion mirrors the original EA logic (`PriceStep * 10` for 3/5-digit symbols) to keep stop-loss distances consistent.
- The StockSharp version evaluates stops and targets once per finished candle. The original EA ran on every tick, therefore the StockSharp port may exit a few ticks later but the logic remains equivalent.
- The strategy explicitly calls `CloseActivePosition` when the session window ends. The MQL script contained the same idea but returned before reaching the closing routine; the C# version implements the intended behaviour.
- Informational logs (`AddInfoLog` / `AddWarningLog`) are used to surface skipped trading periods to the user interface.
