# Non Repainting Renko Emulation Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Traders reach for brick charts because a brick, once printed, never changes: a signal taken on it cannot be taken back a minute later. This diagram gets the same property out of ordinary time candles. The candle source is deliberately subscribed with intermediate updates, and a Final value block stands between it and every block that thinks, so an average, a crossing and an entry are all decided on a bar that has already closed. The live stream is kept, but it goes only to the chart, where a forming candle belongs.

![schema](schema.svg)

## Strategy Overview

- Five-minute candles are subscribed with intermediate updates enabled, so the source emits the bar many times while it is forming and once more when it closes.
- A Final value block of candle type is the only door from the source into the logic: it passes a value on only when that value is final, so nothing downstream ever sees a price that can still move.
- That block is load-bearing rather than decorative. An indicator block marks every value handed to it as final, so an average fed a forming bar would rewrite the same bar's reading on every update, and a crossing built on two such averages would appear and vanish inside the bar.
- A fast and a slow exponential average and a relative strength index are all built on the closed stream, and all three are set to speak only once they are formed, so the first decisions wait for a full slow window.
- Two Crossing blocks carry the two sides of the same event: one has the fast average on the up input and the slow one on the down input, the other has them swapped, so each fires true on the cross it is named after.
- The relative strength index is compared with a midline variable in both directions, giving a momentum filter that must agree with the cross before anything is sent.
- Position is read into a holding variable that is triggered by each closed candle and compared with zero twice, so the entry can require a position that is not long and the exit can require one that is.
- Two logical AND blocks collect cross, momentum and position; one drives a market entry with the Open position condition, the other a market exit with the Close position condition, and the chart panel draws the live candles, all three indicators, the orders and the fills.

## Entry and Exit Rules

- **Long entry**: On a closed candle the fast average crosses above the slow one, the relative strength index stands above its midline and the position is not long. The AND block collects the three answers and the entry block buys the order volume at market. Its Open position condition means the order is sent only from a flat position, so a second cross while a trade is running changes nothing.
- **Short entry**: There are no short entries. Below the midline, or with the fast average under the slow one, the diagram simply stands aside; the only order it ever sends in the sell direction is the one that closes a long.
- **Exit**: On a closed candle the fast average crosses back below the slow one, the relative strength index has fallen under its midline and the position is long. The second AND block fires and the closing block sells the whole position at market, its volume computed from the open position by the Close position condition. There is no stop-loss and no take-profit block: the cross that opened the trade is the same thing that ends it, and every one of those decisions is taken on a bar that has finished.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candle series the whole diagram runs on; the series is subscribed with intermediate updates, and the Final value block sorts the closed bars out of it. |
| Fast EMA Length | 14 | Length of the fast exponential average, the faster of the two lines whose crossings are the signal. |
| Slow EMA Length | 40 | Length of the slow exponential average, the reference line the fast one is measured against. |
| RSI Length | 14 | Length of the relative strength index used as the momentum filter on both sides. |
| RSI Midline | 50 | Level that splits the momentum filter: above it the diagram may open a long, below it a long may be closed. |
| Order Volume | 1 | Order size sent by the entry; the exit takes its size from the open position instead. |

## Diagram Details

- The gate fires exactly on the crossing bar. A Crossing block emits only at the moment the two lines change places, while the comparisons emit on every closed candle; a logical condition holds each input until all of them have arrived since it last fired, so the missing piece is always the cross and the answers it is combined with are the ones from that same bar.
- The two Crossing blocks are the same block with the inputs swapped. Each emits true when its own up input has moved to or above its down input and false at the opposite event, which is why one of them is named for the bullish cross and the other for the bearish one instead of one block feeding both gates.
- The position block speaks only when the position changes, which on a quiet week can be never. The holding variable behind it starts at zero, keeps the last number it was given and re-emits it on every closed candle, so both comparisons always have a fresh left side to work with.
- Nothing spaces the signals out in time. Every qualifying cross is taken, and the only thing that stops one entry landing on top of another is the requirement that the position is not long, backed by the Open position condition on the order block itself.
- The forming candle is not thrown away, it is just kept away from the decisions: the raw stream is drawn on the chart next to the indicators, which are plotted from the closed stream, so the two can be read against each other.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
