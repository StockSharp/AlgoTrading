# Night Session Bollinger Fade Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A band tells you where price has stopped behaving ordinarily; a clock tells you when that is worth acting on. This diagram puts the two together on finished one-hour candles: it fades a touch of a Bollinger band, but only during the evening hours and only while the channel itself is narrow. The way out is the middle line, and that half is not fenced by the clock - a position opened late in the evening is closed the moment price comes back, at whatever hour that happens.

![schema](schema.svg)

## Strategy Overview

- One candle series drives everything. Only finished one-hour candles are emitted, so every comparison, every gate and every order is decided on a closed bar.
- Bollinger Bands over 20 candles with a deviation of 2.0 emit only once formed. Three Converter blocks split the indicator value into the upper band, the lower band and the middle line.
- A Formula subtracts the lower band from the upper one to get the channel width, and a Comparison holds it against the width threshold. That single answer is the quiet-market gate both entries share.
- The Working time block reads the timestamp each candle carries - its opening time - and answers true from 19:00:00 through 23:59:59. Only the two entry gates consult it.
- A long is accepted when four answers agree on the same candle: the low reached the lower band, the channel is inside the threshold, the candle opened inside the session, and the position is flat.
- A short is the mirror image: the high reached the upper band, under the same width, session and flat-position conditions.
- Both entries are market orders sent by Modify position under the Open position condition, so a signal that arrives while a position is already open is refused by the order block itself rather than by an extra comparison.
- Exits compare the close with the middle line and are sent as Reduce only orders in both directions. The chart panel draws the candles, the three band lines, and the orders and fills of all four order blocks.

## Entry and Exit Rules

- **Long entry**: Inside the evening window, on a finished candle whose low reached or crossed the lower band, with the channel width no larger than the threshold and the position flat, Modify position buys the order volume at market under the Open position condition.
- **Short entry**: Inside the same window, on a finished candle whose high reached or crossed the upper band, with the channel width no larger than the threshold and the position flat, Modify position sells the order volume at market under the Open position condition.
- **Exit**: The middle line is the target for both sides. On any finished candle closing at or above it a Reduce only sell is sent, and on any candle closing at or below it a Reduce only buy is sent. A Reduce only block that is handed the direction the position already holds refuses the order on its own, so the selling path is silent while short and the buying path is silent while long. Neither exit is limited by the session or by the channel width - they run on every finished candle, around the clock. There is no stop-loss and no take-profit: the return to the middle line is the only way out of a trade.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 01:00:00 | Time frame of the single candle series the whole diagram runs on; only finished candles leave it. |
| Bollinger Period | 20 | Number of candles the bands are averaged over. |
| Bollinger Deviation | 2.0 | Standard-deviation multiplier that sets how far the two bands sit from the middle line. |
| Width Threshold | 3000 | Widest channel, in the price units of the instrument, that still counts as quiet enough to enter. |
| Session From | 19:00:00 | Start of the window in which entries are allowed, matched against the candle's opening time. |
| Session Until | 23:59:59 | End of that window; a candle that opens at or before it still counts as inside. |
| Order Volume | 0.01 | Order size sent by both entries and by both exits. |

## Diagram Details

- The entry reads the candle's extremes rather than its close. A bar that pierced a band intrabar and closed back inside still counts as a touch, which is what makes this a fade of the excursion instead of a breakout of the closing price.
- The width gate is measured in the price units of the instrument, not in percent. On an instrument quoted in units almost every candle passes it and the gate is effectively open; on one quoted in tens of thousands it becomes the selective filter it is meant to be. The threshold is exposed so it can be matched to the instrument.
- Both exits carry a direction even though Reduce only decides the side itself: the direction is what makes each path refuse the wrong position. That is why no long or short comparison appears on the exit side of the diagram - the two order blocks perform that test.
- One Volume variable feeds all four order blocks. On the exits Reduce only trims the order down to what the position actually holds, so even a partially filled entry is closed exactly rather than reversed.
- The session is evaluated per candle rather than from a free-running clock, so it changes value on the same beat as the band and width answers and the four inputs of an entry gate always describe the same bar.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
