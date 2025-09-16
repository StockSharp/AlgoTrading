# OpenTiks Strategy

## Overview
The OpenTiks Strategy ports the classic MetaTrader expert advisor `OpenTiks.mq4` to the StockSharp ecosystem. The original robot
looked for a staircase of candles with strictly monotonic highs and opens to detect early breakouts. Once a signal emerged, it
opened a market order, optionally attached a protective stop, and then trailed the position while progressively taking profits
by repeatedly halving the exposure. The StockSharp version mirrors those ideas using high-level API calls, candle subscriptions,
and the built-in order helpers so the logic runs inside Designer, Runner, or any custom S# application.

## Pattern detection
A trade can be launched when **four consecutive candles** satisfy one of the following patterns:

- **Bullish breakout** – for the current candle and the previous three bars: each `High` is strictly higher than the preceding
  `High`, and each `Open` is strictly higher than the preceding `Open`.
- **Bearish breakout** – for the same four-bar window: each `High` is strictly lower than the previous `High`, and each `Open`
  is strictly lower than the previous `Open`.

Signals are evaluated on completed candles delivered by the configured `CandleType`. When the breakout condition is met the
strategy sends a market order with the configured volume (normalized to the security’s `VolumeStep` and bounded by `MinVolume`
and `MaxVolume`). The `MaxOrders` parameter limits how many concurrent entries can exist; a value of zero disables the check,
while any positive number blocks new trades once the absolute net position divided by the normalized order volume reaches that
limit.

## Risk and exit management
- **Stop loss** – if `StopLossPoints` is greater than zero, the strategy monitors the latest candle for price reversals. Long
  positions are liquidated when the candle’s low penetrates `entryPrice - StopLossPoints × PriceStep`. Short positions exit when
  the high touches `entryPrice + StopLossPoints × PriceStep`.
- **Trailing stop** – once price advances by at least `TrailingStopPoints × PriceStep` beyond the entry, a trailing stop is armed
  at the same distance behind (for longs) or ahead (for shorts) of the close. Each time the trailing level improves, the
  remaining position is optionally reduced.
- **Progressive profit taking** – when `UsePartialClose` is enabled the strategy closes half of the current exposure every time
  the trailing stop moves forward. Volumes are rounded to the instrument’s `VolumeStep`. If the halved size falls below
  `MinVolume`, the whole position is closed instead, matching the MetaTrader expert’s behaviour.

All stop and trailing calculations are performed on finished candles, so exits occur on the next bar close instead of on every
incoming tick. This keeps the implementation consistent with StockSharp’s high-level API while staying close to the original
idea of reacting to new bars.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `OrderVolume` | `decimal` | `0.1` | Base lot size for each market entry. The strategy normalizes it to the security’s volume step and limits. |
| `StopLossPoints` | `decimal` | `0` | Protective stop distance expressed in price points (price steps). A value of zero disables the stop. |
| `TrailingStopPoints` | `decimal` | `30` | Distance maintained by the trailing stop once the position moves into profit, also in price points. |
| `MaxOrders` | `int` | `1` | Maximum number of simultaneously open entries. Zero removes the restriction. |
| `UsePartialClose` | `bool` | `true` | Enables the halving logic that locks in gains whenever the trailing stop advances. |
| `CandleType` | `DataType` | `1 minute` time-frame | Primary candle subscription used for signal evaluation and trailing checks. |

## Implementation notes
- StockSharp works with **netted positions**, so all orders for the configured security accumulate into a single long or short
  exposure. The `MaxOrders` parameter therefore acts on the aggregated position rather than on individual MetaTrader tickets.
- Candle-based trailing means stop checks happen once per completed bar. Traders who need tick-level protection can reduce the
  candle size or extend the logic to subscribe to trades.
- Partial closures respect the instrument metadata (`VolumeStep`, `MinVolume`, `MaxVolume`) to avoid rejected orders.
- Inline English comments highlight the main decision points so the file doubles as educational material when adapting the idea
  to other break-out or money-management experiments.

## Usage tips
1. Select a candle type that matches the timeframe used in the original MetaTrader setup (for example, M1 or M5).
2. Verify the instrument’s step and lot settings; the default `OrderVolume` of `0.1` suits Forex-style contracts but can be
   adjusted for futures, stocks, or crypto symbols.
3. Experiment with `TrailingStopPoints` and `UsePartialClose` to find a balance between aggressive profit locking and letting
   winners run.
4. Combine the strategy with StockSharp charts to visually confirm the staircase pattern and observe the partial exits in real
   time.
