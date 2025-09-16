# Firebird Channel Averaging Strategy

## Overview
The Firebird Channel Averaging strategy replicates the MetaTrader 5 expert "Firebird v0.60" using StockSharp's high-level API. It trades a configurable moving-average channel and progressively averages into positions when price extends away from the channel. The approach is designed for mean-reversion forex trading where grid-style entries and pip-based risk controls are required.

## Indicator Setup
- A moving average (simple, exponential, smoothed or weighted) is calculated on the selected candle series. The price source (close, high, low, median, etc.) can be configured.
- Upper and lower channel bands are derived by offsetting the moving average by a user-defined percentage.

## Entry Logic
1. **Buy Conditions**
   - Price of the chosen candle source closes below the lower band.
   - Either no position exists, or the new entry is at least `Step (pips)` away from the most recent fill when accounting for the `Step Exponent` growth.
   - The strategy enforces a cooldown of two candle intervals between entries.
2. **Sell Conditions**
   - Price closes above the upper band.
   - Distance and cooldown checks identical to the long logic must be satisfied.

When a valid signal occurs the strategy submits a market order with the configured lot volume. Only one direction is maintained at a time—opposite signals will wait until the current inventory is closed by risk rules.

## Position Management
- Each entry is stored so the strategy can compute the average price of the open grid.
- Stop-loss and take-profit levels are defined in pips. For a single position, the stop loss equals the entry price minus/plus `Stop Loss (pips)` and the take profit equals entry price plus/minus `Take Profit (pips)`.
- When multiple positions exist the stop-loss distance is divided by the number of entries, emulating the averaging behaviour of the original expert.
- Profit targets remain fixed relative to the average price, while stop-loss exits are recalculated on every candle.
- Trading can be optionally disabled on Fridays.

## Parameters
| Parameter | Description |
| --- | --- |
| `Volume` | Order size in lots for every averaged entry (default 0.1). |
| `Stop Loss (pips)` | Protective stop distance in pips (default 50). |
| `Take Profit (pips)` | Take-profit distance in pips (default 150). |
| `MA Period` | Lookback length of the moving average (default 10). |
| `MA Shift` | Forward shift in candles applied to the moving average output. |
| `MA Type` | Moving-average calculation method: Simple, Exponential, Smoothed, or Weighted. |
| `Price Source` | Candle price used for indicator calculations (close by default). |
| `Channel %` | Percentage offset from the moving average used to form the bands (default 0.3%). |
| `Trade Friday` | Enables or disables trading on Fridays. |
| `Step (pips)` | Minimum pip distance between averaged orders (default 30). |
| `Step Exponent` | Exponent that scales the step based on the number of open entries (0 keeps the step constant). |
| `Candle Type` | Timeframe for the working candles. |

## Notes
- The strategy assumes the instrument's `PriceStep` represents one pip. If unavailable it falls back to 0.0001.
- Protective exits are executed with market orders rather than native stop/limit orders to stay consistent with the high-level API.
- The averaging grid is capped by the cooldown logic and by the growing distance when a step exponent greater than zero is used.
