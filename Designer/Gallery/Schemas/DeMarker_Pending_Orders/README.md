# DeMarker Pending Orders Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram turns DeMarker threshold crossings into pullback limit orders instead of entering immediately. Each pending order has a four-candle lifetime, and percentage take-profit and stop-loss protection is attached only after that order actually trades.

![schema](schema.svg)

## Strategy Overview

- Finished fifteen-minute candles feed a DeMarker oscillator with length 14 and a close-price stream.
- The lower and upper thresholds are 0.3 and 0.7; crossing blocks detect a fall through the lower threshold and a rise through the upper threshold.
- Entries are accepted only from 07:00:00 through 20:59:59 on the candle timestamp and only while the position is flat.
- A new setup replaces any older pending entry, while an unfilled order also expires after four subsequent finished candles.
- The chart shows the candles, oscillator, both thresholds and every strategy fill.

## Entry and Exit Rules

- **Long entry**: When DeMarker falls through 0.3 inside the entry window and the position is flat, register a buy limit one pending-indent percentage below the current close.
- **Short entry**: When DeMarker rises through 0.7 inside the entry window and the position is flat, register a sell limit one pending-indent percentage above the current close.
- **Exit**: An unfilled limit is cancelled when another setup replaces it or when its four-candle timer completes. After a fill, position protection closes the position at either the 1.2% take-profit or the 0.6% stop-loss threshold.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| DeMarker Length | 14 | Averaging length of the DeMarker oscillator. |
| Lower level | 0.3 | A downward crossing of this value creates a long setup. |
| Upper level | 0.7 | An upward crossing of this value creates a short setup. |
| Pending indent, % | 0.1 | Distance of the pending price from the signal candle close. |
| Pending life, bars | 4 | Number of later finished candles allowed before an unfilled order is cancelled. |
| Entry window start | 07:00:00 | First candle timestamp accepted by the entry filter. |
| Entry window end | 20:59:59 | Last candle timestamp accepted by the entry filter. |
| Volume | 1 | Size of each pending entry order. |
| Take profit, % | 1.2 | Favourable percentage move from the filled entry price. |
| Stop loss, % | 0.6 | Adverse percentage move from the filled entry price. |
| Candles | 00:15:00 | Time frame of the finished signal candles. |

## Diagram Details

- For the lower crossing, the 0.3 constant is connected to Crossing Input Up and DeMarker to Input Down; for the upper crossing, DeMarker is Input Up and 0.7 is Input Down.
- Two logical AND blocks combine the relevant crossing, the working-time result and the flat-position check; per-candle flags turn each accepted setup into a single trigger.
- Formula blocks calculate `close × (1 − indent / 100)` for Buy and `close × (1 + indent / 100)` for Sell, then the two order-registration blocks submit those prices as limits.
- Each setup starts its own N-values counter. Its output reaches the matching cancellation block after four later candles, and every fresh setup first asks both cancellation blocks to remove older pending entries.
- A Trades for order block watches each registered limit. Only its fill output reaches Position protection, so an order that remains pending or is cancelled cannot arm a stop or take-profit.
- The registration blocks also expose a MyTrade output in current Designer versions; the dedicated Trades for order blocks are retained here to make the order-to-fill event chain explicit.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
