# Session Window Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A breakout is only worth taking while there is someone to trade against. This diagram measures the twenty-candle range, takes a position when a finished candle closes outside it, and refuses to act at all unless that candle belongs to a fixed window of the day. Everything after the entry is left to a percentage stop and take.

![schema](schema.svg)

## Strategy Overview

- A single five-minute candle series drives the whole diagram, and only finished candles are published, so every decision is made on a bar that can no longer change.
- Highest and Lowest, both twenty candles long and both formed-only, carry the upper and lower edge of the recent range.
- Previous value shifts each edge one candle back. That shift is what turns a range into a breakout level: the candle being judged is not allowed to be part of the boundary it has to clear.
- A converter reads the close of the current candle, and two comparisons put it against the two shifted boundaries.
- Working time answers one question per candle - does this candle belong to the trading window - and returns a plain true or false on the same beat as the comparisons.
- Two logical conditions join breakout and window, so a level break outside the window produces nothing at all and the diagram sits idle for the rest of the day.
- Both entries are market orders of a fixed volume and both carry the Open position condition, so an order is sent only from a flat position, never to add to or reverse an existing one.
- Position protection takes the entry fills and the candle close and owns the trade from there, closing it at a fixed percentage of profit or loss.

## Entry and Exit Rules

- **Long entry**: A finished candle closes above the twenty-candle high taken one candle back, and that candle belongs to the trading window. Position modify buys the order volume at market; the Open position condition lets the order through only while the position is flat.
- **Short entry**: A finished candle closes below the twenty-candle low taken one candle back, under the same window condition. Position modify sells the order volume at market, again only from a flat position.
- **Exit**: There is no exit signal in the diagram. Once a position is open, Position protection owns it: it prices the trade off the entry fill, follows the candle close, and closes at 1.5% profit or 0.5% loss - a target three times the risk. The window governs entries only, so a position opened at its very end lives on past the close of the window until one of its two boundaries is reached. An opposite breakout in the meantime is ignored, because the Open position condition blocks any entry that is not from flat.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candle series; only finished candles reach the diagram. |
| Breakout High Length | 20 | Number of candles the upper boundary is measured over, before the one-candle shift is applied. |
| Breakout Low Length | 20 | Number of candles the lower boundary is measured over. Keep it equal to the upper length so both edges describe the same range. |
| Session From | 12:00:00 | Start of the trading window as a time of day. A candle that opened before it cannot trigger an entry. |
| Session Until | 21:00:00 | End of the trading window. A candle that opened after it cannot trigger an entry; a position already open is not affected. |
| Order Volume | 1 | Fixed quantity sent by both market entries. |
| Take Profit, % | 1.5 | Take-profit distance, in percent of the entry price. |
| Stop Loss, % | 0.5 | Stop-loss distance, in percent of the entry price. |

## Diagram Details

- Both range indicators are formed-only and final-only, so no partial reading can move a boundary, and the first twenty candles produce no signal at all.
- The one-candle shift is applied to the indicator output rather than to the price. An indicator value carried back one step is exactly the boundary as it stood before the current candle existed, which is what a breakout has to be measured against.
- Working time reads the timestamp carried by the value it is handed. A candle carries its opening time, so a candle counts as inside the window when it opened inside it.
- The fills of both entries are merged into one stream before they reach Position protection, so a single protective block covers long and short alike.
- Position protection is fed the candle close as its price, so the stop and the take are measured against the same finished-candle prices the entry decision was made on, and are checked once per candle.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
