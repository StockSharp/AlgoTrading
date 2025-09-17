# VLT Trader Strategy

## Overview
The VLT Trader strategy is a StockSharp conversion of the MetaTrader 4 expert advisor "VLT_TRADER". The original idea searches for a period of extremely low volatility and then prepares a breakout straddle around the most recent candle. When the latest completed candle has the smallest range compared with a configurable number of earlier candles, the strategy positions stop orders above and below that candle in anticipation of a volatility expansion.

## Trading logic
- Subscribe to the configured candle series and compute the range (high minus low) for each bar.
- Track the minimum range among the previous `LookbackCandles` bars using the `Lowest` indicator.
- Once the most recent finished candle has a smaller range than this historical minimum, prepare the breakout orders for the following session.
- Place a buy stop above the previous high plus `EntryOffsetPoints` and a sell stop below the previous low minus the same offset.
- Attach fixed-distance stops and targets to every pending order (`StopLossPoints` and `TakeProfitPoints`).
- Leave both pending orders active. Whichever side triggers first becomes a market position, while the opposite stop remains in the book and can activate later if the market reverses.
- When a pending order is filled or cancelled, the corresponding reference is cleared so that new straddles can be created after all positions and orders are closed.

## Risk management
- Trade size is controlled through `OrderVolume` and is rounded to the instrument's volume step and limits.
- Stop loss and take profit distances are expressed in price steps (points) and converted to actual prices using the instrument's `PriceStep`.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Lot size used when creating the pending orders. |
| `EntryOffsetPoints` | Additional points added to the previous high/low when placing stop entries. |
| `TakeProfitPoints` | Take profit distance attached to each order. |
| `StopLossPoints` | Stop loss distance attached to each order. |
| `LookbackCandles` | Number of prior candles used to measure the minimum historical range. |
| `CandleType` | Timeframe of the candle series that feeds the strategy. |

## Notes
- The strategy requires a valid `PriceStep` on the instrument; otherwise no orders are placed.
- Because stop and take-profit levels are transmitted alongside the pending orders, fill prices in StockSharp may differ slightly from MetaTrader depending on broker execution rules.
- The implementation relies exclusively on high-level APIs (`SubscribeCandles` + `Bind`) and the standard `Lowest` indicator to mirror the volatility check from the original EA.
