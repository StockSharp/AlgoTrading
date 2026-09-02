# Larry Connors 3 Day High/Low Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Larry Connors' 3 Day High/Low buys a short pullback inside a rising market. Price has to hold above a slow SimpleMovingAverage, slip under a fast one, and print three candles in a row where both the high and the low are lower than on the candle before. The trade is handed back on the first close above the fast average. The original counts daily bars; this diagram works on five-minute candles so it matches the packaged intraday history.

![schema](schema.svg)

## Strategy Overview

- A candle pattern block carries the whole four-candle shape: three consecutive candles, each with a lower high and a lower low than the one before it.
- A 50-period SimpleMovingAverage decides that the market is rising, so the pullback is only bought on the side of the larger move.
- A 5-period SimpleMovingAverage is both the entry gate, since price under it means the pullback is still running, and the exit trigger.
- The strategy is long only. The original also caps the number of entries and waits fifteen bars between trades; neither counter has a block of its own, so this diagram trades more often than the source.

## Entry and Exit Rules

- **Long entry**: The pattern block reports three lower highs and lower lows, the close is above the slow SMA, the close is below the fast SMA and the position is flat. The order buys the shared volume at market and opens the long.
- **Short entry**: There is no short side. Connors' rule set only buys pullbacks inside a rising market, so the diagram has no sell entry at all.
- **Exit**: The first close above the fast SMA closes the long. The closing block sends a market order sized to whatever is open, and there is no stop loss and no take profit, exactly as in the original code.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Slow SMA Length | 50 | Length of the slow SimpleMovingAverage, the rising-market filter. |
| Fast SMA Length | 5 | Length of the fast SimpleMovingAverage: price under it opens the trade, the first close above it closes it. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the pattern indicator, both moving averages and a converter that reads the closing price.
- Two comparison blocks put the close against the two averages, and the position block is compared against a zero constant.
- One logical AND joins the pattern flag, both average conditions and the flat-position check, then triggers a position modify block set to open a position.
- A second position modify block, set to close a position, is fired by the close returning above the fast average; it takes no volume, because it closes whatever is open.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
