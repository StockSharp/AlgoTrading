# Precipice Strategy

## Overview
The Precipice strategy is a direct conversion of the MetaTrader expert advisor *Precipice (barabashkakvn's edition)*. The system does not analyse market structure or use indicators; instead it waits for the previous position to close and then flips a coin to decide whether to enter long or short. If the trader enables both directions, every finished candle has a 50% chance of spawning a new position provided the account is currently flat. Optional protective orders mirror the MetaTrader behaviour by attaching the same stop-loss and take-profit distance in "pips" to every trade.

The StockSharp implementation keeps the random nature of the original code and mirrors its money-management settings. It automatically converts the MetaTrader pip distance into the instrument's native price step so the stop-loss and take-profit remain symmetrical regardless of the number of decimal places used by the security.

## Trading logic
1. Subscribe to the primary candle series defined by `CandleType` and process only completed candles so the trade timing matches MetaTrader's `OnTick` logic after the bar closes.
2. Ignore all signals while a position is open. The expert places at most one trade at a time.
3. When the strategy is flat, draw a random number for the buy branch. If `UseBuy` is enabled and the draw is below 0.5, submit a market buy order with `TradeVolume` lots.
4. If no long position was opened, draw another random number for the sell branch. When `UseSell` is enabled and the result exceeds 0.5, submit a market sell order.
5. After an entry is filled, attach optional stop-loss and take-profit orders located `StopLossTakeProfitPips` MetaTrader pips away from the candle close. Protective orders are cancelled automatically when the position returns to zero.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute time frame | Primary timeframe processed by the strategy. |
| `TradeVolume` | `decimal` | `1` | Order size used for every market entry. |
| `StopLossTakeProfitPips` | `int` | `100` | Distance (in MetaTrader pips) between the entry price and both protective orders. Set to `0` to disable stop-loss and take-profit placement. |
| `UseBuy` | `bool` | `true` | Enable random long entries. |
| `UseSell` | `bool` | `true` | Enable random short entries. |

## Differences from the original MetaTrader expert
- MetaTrader exposes the instrument's freeze and stop levels; the StockSharp port emulates only the pip-distance conversion and relies on the broker to reject invalid stop distances if necessary.
- The original EA uses the current Bid/Ask quotes. The StockSharp strategy bases protective orders on the candle's closing price because the high-level API receives aggregated candle data; slippage and spread effects must be handled externally.
- MetaTrader works with individual tickets, whereas StockSharp manages net positions. The conversion keeps at most one net position and removes protective orders as soon as the exposure goes back to zero.

## Usage tips
- Choose a realistic `TradeVolume` that matches the security's lot step. The constructor also applies this value to `Strategy.Volume` so helper methods send orders with the intended quantity.
- Adjust `StopLossTakeProfitPips` to match the instrument's volatility. The strategy multiplies pips by the security's price step (scaled for 3/5-digit quotes) to obtain a native price distance.
- Enable only `UseBuy` or `UseSell` if you want the random generator to open trades in a single direction, for example to test directional risk controls.
- Because entries are random, monitor the strategy with additional risk limits or a maximum position duration if you need deterministic exit conditions.

## Indicators
- None. The strategy relies purely on random trade generation and optional protective orders.
