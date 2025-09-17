# Maybeawo222 Strategy

## Overview
The Maybeawo222 strategy replicates the MetaTrader expert advisor "maybeawo222" using StockSharp's high-level API. It trades a single instrument with a simple moving average (SMA) crossover on the previous candle and limits activity to a configurable time window. The conversion keeps the staged breakeven management that attempts to lock in profits as soon as the price advances by predefined distances.

## Trading Logic
1. The strategy subscribes to the main candle series selected through `CandleType` and calculates a simple moving average with the period specified by `MovingPeriod`.
2. At the close of every candle the SMA value is shifted by `MovingShift` bars before being used in the decision. This reproduces the original `iMA` call with a shift parameter.
3. Trading signals are only evaluated when the closing time of the finished candle falls inside the `[StartHour, EndHour)` range. Outside of that window no new orders are created, although open positions continue to be managed.
4. A **buy** signal appears when the previous candle (the one that just closed) opens below the shifted SMA and closes above it. A **sell** signal requires the opposite crossover. The strategy reverses existing positions if necessary so only one direction remains open.
5. On every finished candle the engine checks the high/low extremes to detect stop-loss or take-profit hits. Whenever either level is touched, the corresponding market exit is triggered immediately.
6. The position also activates up to two staged breakeven adjustments. Once the floating profit exceeds `BreakevenPips1`, the stop moves closer to the entry according to `DesiredBreakevenDistancePips1`. A second stage repeats the process with `BreakevenPips2` and `DesiredBreakevenDistancePips2`.

## Risk Management
- Initial stop-loss and take-profit distances are configured in pips. The conversion uses the instrument `PriceStep` and applies the conventional MetaTrader factor of 10 for three- and five-digit quotes.
- Breakeven levels are only applied once per position side. Each new entry resets the flags, allowing the stop to trail twice during the lifetime of the trade.
- Position exits use market orders so the engine can close trades even if the stop or target levels are not available on the broker side.

## Parameters
| Name | Default | Range / Notes | Description |
|------|---------|---------------|-------------|
| `MovingPeriod` | `14` | Positive integer | SMA length used for the crossover check. |
| `MovingShift` | `0` | `0` – `10` (suggested) | Number of completed candles to shift the SMA value backwards. |
| `StopLossPips` | `100` | `0` disables | Distance from the entry price to the protective stop-loss, measured in pips. |
| `TakeProfitPips` | `800` | `0` disables | Distance from the entry to the take-profit level, measured in pips. |
| `BreakevenPips1` | `180` | `0` disables | Profit threshold (in pips) that triggers the first breakeven adjustment. |
| `DesiredBreakevenDistancePips1` | `60` | Any non-negative | New stop distance from the entry after breakeven stage 1 fires. |
| `BreakevenPips2` | `500` | `0` disables | Profit threshold (in pips) that triggers the second breakeven adjustment. |
| `DesiredBreakevenDistancePips2` | `350` | Any non-negative | New stop distance from the entry after breakeven stage 2 fires. |
| `StartHour` | `3` | `0` – `23` | Inclusive start hour of the trading session, based on exchange time. |
| `EndHour` | `22` | `0` – `23` | Exclusive end hour of the trading session. |
| `OrderVolume` | `0.5` | Greater than `0` | Volume sent with every market order before position netting. |
| `CandleType` | `H1` | Any candle data type | Candle series used for generating signals and calculating the SMA. |

## Notes for Use
- Ensure the connected security provides a valid `PriceStep`; otherwise the pip conversion falls back to `1`. Adjust the pip-related parameters accordingly if your instrument quotes in large ticks.
- The strategy expects a single-symbol setup. Add it to a scheme with the desired instrument before starting the strategy.
- For live trading consider enabling slippage allowances or protective stop orders through broker-specific extensions if market exits are not sufficient.
