# Show Pips Strategy

## Overview
- **Source**: Converted from the MetaTrader 5 indicator `Show Pips.mq5` (TPSproTrading, 2017).
- **Purpose**: Visual monitoring tool that reports the floating profit of the current symbol, spread and the remaining time until the next candle close.
- **Type**: Informational strategy (no automated orders).

## How it works
1. Subscribes to the configured candle series to track bar timing.
2. Subscribes to Level1 data to obtain last trade, bid and ask prices for spread estimation.
3. Calculates:
   - Open profit in **pips**, **account currency** and **percentage of portfolio equity**.
   - Current **spread** in ticks (falls back to price units if tick size is missing).
   - Countdown until the next bar closes using the selected timeframe.
4. Outputs the composed status line either to the chart or to the strategy log depending on the selected mode.

All computations rely on StockSharp high level API: indicator bindings are not required, and the strategy only reacts to finalized candles for the timing information.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `ShowType` | Display mode: follow price on the chart, log comment, or fixed corner label. | `FollowPrice` |
| `ShowProfit` | Append currency profit to the status line. | `false` |
| `ShowPercent` | Append percentage profit relative to the current portfolio value. | `false` |
| `ShowSpread` | Append current spread (ticks or price units). | `true` |
| `ShowTime` | Append countdown until the next candle closes. | `true` |
| `Separator` | Text separator between status blocks. | `<code> | </code>` |
| `CandleType` | Candle data type used to measure the bar duration. | `TimeFrame(1m)` |
| `PipSize` | Price change that corresponds to one pip. | `0.0001` |

## Display modes
- **FollowPrice** – draws the status text next to the current price (updates on each data change).
- **AsComment** – prints the status string into the strategy log (duplicates are filtered).
- **CornerLabel** – draws the status text slightly above the current price to mimic a fixed overlay.

## Notes
- Profit in pips respects the side of the open position (positive when the trade is in profit).
- Percent profit uses `Portfolio.CurrentValue`; set up the simulator or real connector so that this field is available.
- When no position is open the strategy still reports spread and time to close, while the profit block shows `0.0 pips`.
- The strategy does **not** send any trading orders.

## Original indicator reference
The MetaTrader version displayed similar information by drawing text or using the comment section. This conversion reproduces the behaviour in StockSharp while leveraging chart drawing helpers and the Level1 stream.
