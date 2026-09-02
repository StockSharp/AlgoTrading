# Fibonacci Retracement Reversal Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The swing range of the last twenty candles is split by the golden ratio, and the two resulting retracement levels are used as reversal zones. A candle that closes on the lower level in a bullish body is bought, a candle that closes on the upper level in a bearish body is sold, and the SimpleMovingAverage decides when the trade is over.

![schema](schema.svg)

## Strategy Overview

- Highest and Lowest over the same lookback give the swing high and the swing low; their difference is the range the levels are measured in.
- The buy level sits 0.618 of the range under the swing high, the sell level 0.618 of the range above the swing low, and a candle counts as being on a level while its close is within two percent of the range of it.
- Both distances are computed relative to the range, so the diagram works the same on any instrument and any price scale.
- Entries also need a confirming candle body and a flat position; the SimpleMovingAverage handles every exit, because the original strategy sets no stop and no target.

## Entry and Exit Rules

- **Long entry**: The close is within the buffer around the lower retracement level, the candle is bullish (close above open) and the position is flat. The block buys one lot and opens a long.
- **Short entry**: The close is within the buffer around the upper retracement level, the candle is bearish (close below open) and the position is flat. The block sells one lot and opens a short.
- **Exit**: A long is closed as soon as a candle closes below the SimpleMovingAverage, a short as soon as one closes above it; both exit blocks run in close-position mode, so they only fire when there is something to close.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Swing lookback | 20 | Number of candles the swing high and the swing low are taken over. |
| MA period | 20 | Length of the SimpleMovingAverage the exits are measured against. |
| Fibonacci ratio | 0.618 | Retracement ratio that places both levels inside the swing range. |
| Level buffer | 0.02 | Half-width of the entry zone around a level, as a share of the swing range. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- One candle block feeds Highest, Lowest and the SimpleMovingAverage, plus two converters that pull the close and the open out of the candle.
- Two formula blocks turn the raw prices into the distance from the close to each level, divided by the range, so a single buffer constant serves both sides.
- Every entry passes through a logical AND of three flags: the level, the candle body and the position compared with a zero constant.
- The two exit blocks are triggered straight from the moving-average comparisons and are set to close-position mode; all four order blocks share one volume constant.
- Deliberate simplifications: the original works on one-minute candles and pauses for 500 bars after every trade, which no block can express, so the diagram runs on five-minute candles and trades again as soon as the conditions return. Expect it to hold positions for a handful of bars instead of days; raising the MA period lengthens them.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
