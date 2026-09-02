# Pivot Point Reversal Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The classic floor-trader pivot is rebuilt on every candle from a rolling window: the highest high and the lowest low of the last sixty candles together with the current close give the pivot P, the support S1 and the resistance R1. The diagram fades the edges of that band and takes the money back at the pivot.

![schema](schema.svg)

## Strategy Overview

- Highest and Lowest over the same window replace the previous session's range, so the levels move with the market instead of being fixed once a day.
- P = (High + Low + Close) / 3, S1 = 2P - High, R1 = 2P - Low, and a buffer of two percent of the window range widens both zones.
- An entry also needs the candle to agree with the direction: a bullish candle at support, a bearish candle at resistance.
- The pivot itself is the target: the position is closed as soon as the close crosses to the other side of P.

## Entry and Exit Rules

- **Long entry**: The candle low reaches into the S1 zone (low <= S1 + buffer), the candle closes above its open, and the position is flat. The buy order opens a long of one lot.
- **Short entry**: The candle high reaches into the R1 zone (high >= R1 - buffer), the candle closes below its open, and the position is flat. The sell order opens a short of one lot.
- **Exit**: A long is closed when the close is above the pivot, a short when the close is below it. Both exit blocks work in close-position mode, so they stay idle when there is nothing to close. The original code has neither a stop-loss nor a take-profit, and the diagram keeps it that way.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Highest Length | 60 | Window length of the Highest indicator, the number of candles the window high is taken from. |
| Lowest Length | 60 | Window length of the Lowest indicator; keep it equal to the Highest length. |
| Zone Buffer | 0.02 | Width of the entry zones as a share of the window range, 0.02 being two percent. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the Highest and Lowest indicators as well as four converter blocks for open, high, low and close.
- Three formula blocks turn those five numbers into the pivot, the buffered support and the buffered resistance; the buffer constant is a separate block, so it can be optimized.
- Each entry is a logical AND of three comparisons: the level touch, the candle direction and a flat position.
- The two exit blocks are triggered by a plain comparison of the close against the pivot and use the close-position mode instead of a fixed volume.
- The original strategy runs on one-minute candles and pauses for five hundred bars after every trade; the diagram works on five-minute candles, which the packaged history supports, and has no such pause.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
