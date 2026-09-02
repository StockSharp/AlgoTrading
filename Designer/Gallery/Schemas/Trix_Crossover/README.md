# TRIX Crossover Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

TRIX here is not an off-the-shelf indicator but a series built in the diagram, exactly as the original strategy builds it: a triple exponential average and its one-bar relative change. The fast series crossing zero is the trigger, the slow series has to be moving in the same direction by more than a threshold, and a percent take and stop close the trade.

![schema](schema.svg)

## Strategy Overview

- Two triple exponential averages of the closing price, 9 and 21 bars, are the raw material; a previous-value block holds each of them one candle back.
- The slow TRIX is a formula block: the average minus its previous value, divided by that previous value, which is the relative change per bar the original computes in code.
- The fast TRIX crossing zero is drawn as the crossing of the fast average with its own previous value. Because a price average is positive, the sign of the relative change is the sign of the difference, so the crossing block is an exact substitute and saves the division.
- The threshold on the slow TRIX is what keeps the diagram out of a flat market: a turn of the fast series is only accepted while the slow one is moving by more than 0.05 percent per bar in the same direction.
- The original runs on four-hour candles with a take of 1500 and a stop of 500 in absolute price units; the diagram is scaled to five-minute candles for the packaged sample history, and the two distances become percentages of the entry price in the same three-to-one ratio.
- The built-in Trix indicator is deliberately not used: it is a chain of three successive smoothings scaled by a constant, so its values and signals differ from the triple exponential average the strategy is written on.

## Entry and Exit Rules

- **Long entry**: The fast TRIX crosses zero upwards, that is the fast triple exponential average turns up after falling, the slow TRIX is above the threshold, and the position is not long. The order buys one lot at market, which opens a long from flat or closes an equal short.
- **Short entry**: The fast TRIX crosses zero downwards, that is the fast triple exponential average turns down after rising, the slow TRIX is below the negative threshold, and the position is not short. The order sells one lot at market, which opens a short from flat or closes an equal long.
- **Exit**: The position protection block closes the trade on the take profit or the stop loss, both measured in percent of the entry price; otherwise the position is held until the opposite signal, which closes it because every order uses the same volume.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Fast TEMA length | 9 | Length of the fast triple exponential average the trigger series is built on. |
| Slow TEMA length | 21 | Length of the slow triple exponential average the confirmation series is built on. |
| Volume | 1 | Order volume, in lots; the same constant feeds both order blocks. |
| Take profit, % | 1.5 | Take profit distance, in percent of the entry price. |
| Stop loss, % | 0.5 | Stop loss distance, in percent of the entry price. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- A converter block reads the closing price out of the candle and feeds both indicator blocks, and the same value is passed to the protection block as the current price.
- Each average has a previous-value block behind it; the fast pair goes into a crossing block, the slow pair into a formula block that divides the difference by the previous value.
- The crossing block signals the upward turn and a NOT block inverts it into the downward one; two comparison blocks put the slow series against the positive and the negative threshold constants.
- Each logical AND joins the turn, the confirmation and a position check, then triggers a position modify block; both blocks send their own trade to the position protection block.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
