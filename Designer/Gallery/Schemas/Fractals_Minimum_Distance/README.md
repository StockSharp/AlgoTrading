# Fractals Minimum Distance Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A fractal is a candle that stands higher, or lower, than the two candles on either side of it, and it can only be named once those later candles exist. This diagram finds both kinds, remembers the price of the most recent one of each colour, and refuses to trade until the two of them are far enough apart to be worth trading between. Everything is measured on finished candles, so a level is settled the moment it is claimed and is never revised afterwards.

![schema](schema.svg)

## Strategy Overview

- One candle series feeds the entire diagram, and it delivers finished candles only, so no level, no distance and no order is ever built from a price a later tick could still take back.
- Previous value hands back the candle one bar ago, and Highest and Lowest measure the extreme of the five candles that end there — a window lying entirely in the past.
- A second Previous value hands back the candle three bars ago, the exact centre of that window, and two converters read its high and its low.
- When the centre high matches the highest of the window the diagram has an upper fractal, and the mirror test on the low marks a lower one. A latching variable stores each fractal price, and because a latch ignores a false comparison, the stored price only changes on a bar that really printed a fractal.
- A second pair of variables re-emits the two stored levels on every candle, so the formula that measures the gap between them and the comparison against the minimum distance both produce an answer on every bar, not only on fractal bars.
- Each side joins its own fractal test with that distance test in a logical condition; the accepted signal first closes an opposite position and only then opens a new one, both by market.
- Position protection follows every fill and prices its exit from the close of each finished candle, so a take-profit or a stop-loss is checked once a bar.
- The candles, both extremes, both stored levels and every fill are drawn on one chart area, so the two levels and the gap between them can be read straight off the picture.

## Entry and Exit Rules

- **Long entry**: A lower fractal is confirmed — the low three bars back is the lowest of the five-candle window ending one bar back — and the distance between the latest upper level and the latest lower level is at least the minimum distance. The diagram closes a short position if one is open, then buys the order volume at market.
- **Short entry**: An upper fractal is confirmed — the high three bars back is the highest of the same window — under the same distance condition. The diagram closes a long position if one is open, then sells the order volume at market.
- **Exit**: Two things can end a trade. A fractal of the opposite colour closes what is open before the new entry is sent, which is why a closing block sits ahead of the opening one on each signal. Independently of that, position protection closes the position at a take-profit or a stop-loss measured in percent of the fill price, checked against the close of each finished candle.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the single candle series. Only finished candles are delivered, so a decision is taken once per bar. |
| Upper Fractal Length | 5 | Number of candles in the window whose highest high the centre candle is measured against. |
| Lower Fractal Length | 5 | Number of candles in the window whose lowest low the centre candle is measured against; keep it equal to the upper one so the fractal stays symmetric. |
| Fractal Shift | 3 | How many bars back the centre candle sits. With a window of five delayed by one bar, three places the centre exactly in the middle of it. |
| Minimum Distance | 100 | Smallest gap between the latest upper level and the latest lower level that still allows an entry. It is an absolute distance in the price units of the instrument, so it has to be rescaled whenever the diagram is moved to an instrument quoted at a different order of magnitude. |
| Order Volume | 1 | Order size, in lots. |
| Take Profit, % | 2 | Take-profit distance, in percent of the fill price, checked on the close of each finished candle. |
| Stop Loss, % | 1 | Stop-loss distance, in percent of the fill price, checked on the close of each finished candle. |

## Diagram Details

- The indicator block stamps whatever reaches it as a completed value — it has no way of telling a forming candle from a closed one. Delivering finished candles only is what keeps half-finished highs and lows out of the window, where they would quietly move the extreme and then be overwritten on the next update.
- The same subscription is what keeps the orders legal: an order built from an update of an unfinished candle carries that bar's opening time and is refused as arriving from the past.
- The window is delayed by one bar so that the candle being judged sits exactly in the middle of it, with two candles before and two after. A fractal is therefore never claimed earlier than three bars after it happened, and it is never revised afterwards.
- The latch trigger drops a false comparison instead of storing a value, which is what turns 'the test passed on this bar' into 'this is the last price at which it passed'. Neither level exists until its first fractal, so no entry is possible before both colours have been seen.
- Both entry blocks trade at market and are told not to require the strategy to be online, so the diagram behaves the same way on recorded history as it does in live trading.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
