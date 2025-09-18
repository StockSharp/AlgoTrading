# SMC Hilo MaxMin Breakout Strategy

## Overview
This strategy reproduces the behaviour of the MetaTrader expert *SMC MaxMin at 1200*. At the specified terminal hour it places a
buy-stop order above the previous candle's high and a sell-stop order below the previous candle's low. Pending orders are padded
by the broker's minimum stop distance, converted from pips to instrument price units. Once a breakout occurs the opposite order
is cancelled and the open position is managed through fixed stops, profit targets and an optional trailing stop.

Key differences versus the original MQL4 code:

- StockSharp order primitives (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) replace the direct `OrderSend` calls.
- The minimum stop distance, stop-loss and take-profit inputs are expressed in pips and converted through `Security.PriceStep` to
  respect the actual instrument tick size.
- Trailing stop management moves the stop order only when a profitable distance larger than the trailing buffer is achieved.
- All logic is driven by the high-level candle subscription API, so no direct history scans or manual indicator buffers are used.

## Trading Rules
1. **Setup hour** – when the terminal hour equals `SetHour`, use the previous completed candle for reference.
2. **Long entry** – place a buy-stop at `previous_high + min_stop_distance + price_step`.
3. **Short entry** – place a sell-stop at `previous_low - min_stop_distance - price_step`.
4. **Mutual exclusivity** – if either stop is filled, the opposite pending order is cancelled immediately.
5. **Stop-loss** – the long stop is `previous_low - StopLossPips`, the short stop is `previous_high + StopLossPips` (both converted
   to price units).
6. **Take-profit** – long positions use a sell-limit at `entry + TakeProfitPips`; short positions use a buy-limit at
   `entry - TakeProfitPips`.
7. **Trailing stop** – when a position is in profit by more than `TrailingStopPips`, the stop is trailed to keep the same pip
   distance from the current bid/ask.
8. **Order timeout** – two hours after the setup (`SetHour + 2`), any unfilled pending stops are cancelled.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `Volume` | Order volume used for both entry orders. | `0.1` |
| `SetHour` | Terminal hour (0–23) when the breakout straddle is created. | `15` |
| `TakeProfitPips` | Profit target distance in pips. Set to `0` to disable take-profit orders. | `500` |
| `StopLossPips` | Protective stop distance in pips. Set to `0` to disable the initial stop. | `30` |
| `TrailingStopPips` | Distance for the trailing stop in pips. Set to `0` to keep a static stop. | `30` |
| `MinStopDistancePips` | Broker minimum stop distance used to pad entry prices. | `0` |
| `CandleType` | Candle type that defines the hourly session, defaults to 1-hour time frame. | `1h` |

## Usage Notes
- The strategy requires level-1 data to manage trailing stops and to keep the latest bid/ask prices for distance calculations.
- If the underlying instrument has non-standard tick sizes (e.g. JPY crosses with 0.01 pip), adjust `TakeProfitPips`,
  `StopLossPips` and `TrailingStopPips` accordingly.
- When `TakeProfitPips` or `StopLossPips` is zero the respective orders are not submitted, but trailing stops may still activate if
  the trailing parameter is positive.
- Ensure that the configured `SetHour` matches the broker server time of the incoming data feed.
