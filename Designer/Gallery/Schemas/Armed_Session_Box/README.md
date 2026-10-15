# Armed Session Box Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A quiet night usually ends somewhere. This diagram measures the range the market kept between two night hours, waits for the trading session to open, and arms itself once — freezing the top and the bottom of that box as the two prices it will trade. From then on it watches the best quotes of the order book and buys or sells the moment one of the frozen levels is reached.

![schema](schema.svg)

## Strategy Overview

- Five-minute candles are subscribed with intermediate updates, and a Final value block lets only closed candles through; every decision in the diagram is taken on a finished bar.
- A Working time block marks the measuring window, and a Variable of candle type is triggered by that signal, so only candles that fall inside the night window reach the indicators — the box is built from that window and from nothing else.
- Highest and Lowest over the gated stream are the top and the bottom of the box, and two variables hold the last readings so the rest of the diagram can look at the box on any candle, hours after it was measured.
- Market depth is read for the best ask and the best bid, and two more variables hold those quotes at the candle beat: the book updates hundreds of times per bar and would never line up with a condition built on candles.
- Formulas turn the box into its width as a percentage of price and into an edge margin measured off that width, so the same two settings mean the same thing on an instrument priced in tens of thousands and on one priced in units.
- A second Working time block opens the trading session, and a logical condition collects four answers: the box is narrow, the ask sits clear of the top, the bid sits clear of the bottom, and the position is flat.
- The Flag turns that condition into a single arming event per session and freezes both levels; the box behind them may be redrawn the next night, the armed levels will not move.
- Entries are market orders from a flat position when a held quote reaches its frozen level, and Position protection carries the trade from there.

## Entry and Exit Rules

- **Long entry**: Inside the session, with the box armed and the position flat, the best ask held at the candle reaches the frozen top level. Position modify buys the order volume at market.
- **Short entry**: Inside the same session and under the same armed and flat conditions, the best bid held at the candle falls to the frozen bottom level. Position modify sells the order volume at market.
- **Exit**: There is no exit signal on the diagram. Position protection owns the trade once it is open and closes it at a take-profit or a stop-loss of one percent from the entry price, reading the current price from the order book rather than from a candle. An accepted entry also writes the armed state back to zero, so a session gives one trade and the next arming waits for the next day.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candle series the whole diagram runs on. |
| Box From | 02:00:00 | Start of the window in which the box is measured. |
| Box Until | 07:59:59 | End of the measuring window; the last candle that opens before it still counts. |
| Box Top Length | 72 | How many candles of the measuring window the box top is taken over. |
| Box Bottom Length | 72 | How many candles of the measuring window the box bottom is taken over. |
| Max Box Width, % | 3 | Widest box, as a percentage of price, that is still considered quiet enough to trade. |
| Edge Margin, % | 20 | How far from an edge the price has to sit at the moment of arming, as a percentage of the box height. |
| Session From | 08:00:00 | Start of the session in which arming and entries are allowed. |
| Session Until | 20:00:00 | End of the session; after it the flag is released and the armed state is cleared. |
| Order Volume | 0.01 | Order size sent by both entries. |
| Take Profit, % | 1 | Take-profit distance, in percent of the entry price. |
| Stop Loss, % | 1 | Stop-loss distance, in percent of the entry price. |

## Diagram Details

- The box is a rolling Highest and Lowest of the configured length over the candles that fell inside the measuring window, not a range rebuilt from scratch every night. Early in the window the tail of the previous night is still in the buffer; by the end of the window the readings are exactly the night just measured, which is when they are used.
- The armed state is kept as a number, not as the output of the Flag. The Flag emits its single true and then stays silent, so a variable written to one on arming, to zero when the session closes and to zero after an entry is what the entry conditions can read on every candle.
- Every value that enters a comparison or a logical condition is re-emitted on each closed candle through a holding variable. A comparison fires only when both of its sides have arrived since the last time it fired, so a level captured once per day has to be handed on again bar by bar.
- Arming is checked on every candle of the session, not only at its first minute: the first moment the price sits comfortably inside a narrow box is the moment the levels are frozen. Arming and the first possible entry are therefore always at least one candle apart.
- Entries are market orders on the touch of a level, and the protective exit is a percentage of the entry price rather than the opposite edge of the box, so both sides of the trade are expressed in the same units and survive a change of instrument.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
