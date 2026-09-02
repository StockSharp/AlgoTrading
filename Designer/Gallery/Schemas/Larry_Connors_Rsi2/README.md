# Larry Connors RSI-2 Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Larry Connors' RSI-2 buys panic and sells euphoria, but only on the side the slower average allows: a two-period RSI marks the extreme, a 50-period SMA decides the direction, and a 5-period SMA times the exit. The original trades four-hour candles; this diagram works on five-minute candles so it matches the packaged intraday history.

![schema](schema.svg)

## Strategy Overview

- RSI with a length of two reacts to a single candle, so a reading under 6 or over 95 marks a short burst of selling or buying rather than a lasting condition.
- The slow SMA is a direction filter: longs are taken only above it and shorts only below it, which keeps the diagram on the side of the larger move.
- A position is opened only from flat, and the fast SMA closes it as soon as price steps back over that average, so trades usually live one or two candles.
- A protection block adds a percentage stop and target in place of the pip-based stop and target of the original, which cannot be computed from the price step inside a diagram.

## Entry and Exit Rules

- **Long entry**: RSI(2) is below the long entry level, the close is above the slow SMA and the position is flat. The order buys the shared volume at market and opens the long.
- **Short entry**: RSI(2) is above the short entry level, the close is below the slow SMA and the position is flat. The order sells the shared volume at market and opens the short.
- **Exit**: A long is closed when the close returns above the fast SMA and a short when the close falls below it; the 1% stop or the 2% target closes the position earlier if price reaches one of them first.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| RSI Length | 2 | Averaging length of the Relative Strength Index; two candles by design. |
| Fast SMA Length | 5 | Length of the fast SMA that times the exit. |
| Slow SMA Length | 50 | Length of the slow SMA that decides which side may be traded. |
| RSI Long Entry | 6 | RSI level under which a long is allowed. |
| RSI Short Entry | 95 | RSI level above which a short is allowed. |
| Take Profit, % | 2 | Take profit distance from the entry price, in percent. |
| Stop Loss, % | 1 | Stop loss distance from the entry price, in percent. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the RSI, both moving averages and a converter that reads the close price of every finished candle.
- Six comparison blocks carry the rules: two put RSI against its entry levels, two put the close against the slow SMA and two put the close against the fast SMA.
- Both entry ANDs also take the flat-position check, and the entry blocks are set to open a position, so a signal never adds to a trade that is already running.
- The exit blocks are set to close a position, so each of them acts only when a position of the opposite side exists, and every own trade is routed into the protection block so its stop and target follow the real position.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
