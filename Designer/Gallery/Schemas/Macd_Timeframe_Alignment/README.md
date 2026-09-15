# MACD Timeframe Alignment Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

One MACD is an opinion; two of them on different timeframes agreeing is a signal. The diagram measures how far MACD sits from its own signal line on half-hour candles and on four-hour candles, and opens a position only when both readings point the same way and the order book is tight enough to trade in.

![schema](schema.svg)

## Strategy Overview

- Two candle blocks work on the same instrument at two time frames: half an hour for trading and four hours for confirmation.
- Each series feeds its own MACD, and two converters take the MACD line and the signal line out of each indicator.
- A formula subtracts the signal line from the MACD line, so each timeframe is reduced to one number: positive means the fast side leads, negative means it lags.
- Market depth is cut to its best level and two converters read the best ask and the best bid; a third formula turns them into the spread.
- Sync holds all three numbers and releases them together on the four-hour beat. That is what makes the comparison honest: the book updates hundreds of times per bar, and without it the readings would never belong to the same moment.
- After Sync the two gaps are compared with zero and the spread with its limit, and a logical condition collects the three answers together with a flat position.
- Both entries are market orders of a fixed volume, taken only from a flat position.
- Position protection carries the trade: it watches the entry fills, reads the book for the current price and closes at a take-profit or a stop-loss expressed in percent.

## Entry and Exit Rules

- **Long entry**: On the four-hour beat both gaps are positive — MACD is above its signal line on the trading timeframe and on the confirming one — the spread is inside its limit and the position is flat. Position modify buys the order volume at market.
- **Short entry**: On the same beat both gaps are negative, under the same spread and flat-position conditions. Position modify sells the order volume at market.
- **Exit**: There is no exit signal in the diagram: once a position is open, Position protection owns it and closes it at 1.5% profit or 1% loss from the entry price. Opposite readings are ignored while the position lives, so a trade is never reversed mid-flight.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Trading Candles | 00:30:00 | Time frame the trading MACD works on. |
| Confirming Candles | 04:00:00 | Time frame of the confirming MACD, and the beat Sync releases everything on. |
| Maximum Spread | 50 | Widest spread, in price units, that still allows an entry. |
| Order Volume | 1 | Order size, in lots. |
| Take Profit, % | 1.5 | Take-profit distance, in percent of the entry price. |
| Stop Loss, % | 1 | Stop-loss distance, in percent of the entry price. |

## Diagram Details

- Both MACD blocks are set to emit only formed and final values, so an unfinished candle cannot move the decision.
- The confirming MACD is deliberately shorter than the trading one: four-hour candles are scarce in a month of history, and the standard 12/26/9 would spend most of it warming up.
- Sync names each line it holds, and both ends of every line are linked — a value sent in and never taken out would leave the block waiting and the strategy would not start.
- The spread is compared after Sync rather than where it arrives, which is the only reason a book-driven filter can sit in a candle-driven condition at all.
- Position protection is fed from the order book rather than from a candle price, so it prices the exit off the current best level.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
