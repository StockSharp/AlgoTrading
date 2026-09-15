# Envelope Multi Cross Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A fast average and a slow average do not meet at one clean point: they touch, part and touch again around the same zone. This diagram accepts that and turns the zone into three levels — a narrow band drawn above and below the slow average, and the slow average itself. Every level gets its own crossing block, and two combination blocks funnel six independent crossings into one long stream and one short stream, so a whole ladder of signals arrives at a single pair of order blocks.

![schema](schema.svg)

## Strategy Overview

- Finished candles of one instrument feed two exponential moving averages, a fast one and a slow one, and a converter that reads the closing price out of the same candle.
- Two formulas take the slow average and scale it up and down by a fixed fraction, producing an upper and a lower envelope; with the slow average between them, the diagram has three levels instead of one.
- Three crossing blocks watch the fast average rise through the lower envelope, through the slow average and through the upper envelope, each on its own pair of inputs.
- Three more crossing blocks carry the same three levels with the inputs swapped — level above, fast average below — so they report the fast average sinking through those levels.
- One combination block joins the three upward crossings into a single stream and a second joins the three downward ones; from that point on the whole ladder is one signal per side.
- Each stream is confirmed by a comparison of the closing price against the fast average, so a piercing counts as an entry only while price is on the same side of the fast line.
- Entries are market orders of a fixed volume taken from a flat position; the opposite stream, used raw, drives a close-position block on its own.
- Position protection takes over every entry fill and carries it with a percentage take-profit and a trailing stop-loss.

## Entry and Exit Rules

- **Long entry**: The long stream fires: the fast average has crossed above the lower envelope, above the slow average or above the upper envelope. The comparison confirms that the candle closed above the fast average, the two answers meet in a logical condition, and the position modify block buys the order volume at market. The open-position setting lets that order through only while the position is flat.
- **Short entry**: The short stream fires in the same way, on the mirrored crossings: the fast average has sunk through the upper envelope, through the slow average or through the lower envelope. The comparison confirms that the candle closed below the fast average, and the position modify block sells the order volume at market from a flat position.
- **Exit**: Two independent things end a trade. The opposite stream, taken without the price confirmation, triggers a close-position block: any downward piercing flattens a long, any upward piercing flattens a short. Meanwhile position protection watches the entry fills, reads the closing price and closes the trade at the take-profit or at the trailing stop — whichever is reached first.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:15:00 | Time frame of the candle series everything else is calculated on. |
| Fast EMA Length | 10 | Length of the fast exponential moving average — the line that does the piercing. |
| Slow EMA Length | 30 | Length of the slow exponential moving average — the line the band is drawn around. |
| Envelope Buffer | 0.003 | Half-width of the band as a fraction of the slow average: 0.003 puts the envelopes 0.3% above and below it. |
| Order Volume | 1 | Order size, in lots, for both entry directions. |
| Take Profit, % | 1.5 | Take-profit distance, in percent of the entry price. |
| Stop Loss, % | 0.8 | Stop-loss distance, in percent of the entry price; it trails behind a position that moves in favour. |

## Diagram Details

- The two combination blocks are what makes the ladder readable. Without them each of the six crossings would need its own wire to the order blocks, and adding a fourth level would mean redrawing the whole right-hand side of the diagram.
- A crossing block reports the direction it crossed in, and a downward crossing arrives as a negative signal that an order trigger quietly ignores. That is why the downward set is built as three separate blocks with the inputs swapped, rather than by negating the upward set.
- Candles are subscribed as finished only. An update of a still-forming candle carries the timestamp of the bar's opening, and an order stamped earlier than the current moment is refused.
- Entries use the open-position condition, so a signal arriving while a trade is already running costs nothing: the block reports an invalid volume and no order is sent. That one setting does the work of an explicit position filter inside the entry condition.
- The band is deliberately narrow. It is a buffer around the slow average, not a volatility channel, so the two extra levels fire close to the plain crossover and thicken the signal instead of replacing it.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
