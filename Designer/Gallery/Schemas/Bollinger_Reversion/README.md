# Bollinger Reversion Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A close outside a Bollinger band is treated as a stretch that is about to be given back: the diagram buys under the lower band, sells above the upper one and holds the position only until the price touches the middle line again. Unlike a breakout diagram on the same bands, it enters against the move and takes the middle line, not the opposite band, as its target.

![schema](schema.svg)

## Strategy Overview

- BollingerBands is calculated once and read three times: upper band, lower band and the moving average in the middle.
- An entry is made only from a flat position, so a run of closes outside the band adds nothing to a position already taken.
- The exit is symmetric to the entry: the middle line is the target, and the closing block sends exactly the size of the open position.
- The width of the bands and their period are exposed, so the same diagram serves a quiet instrument and a volatile one.

## Entry and Exit Rules

- **Long entry**: The candle closes below the lower band and the position is flat. The order buys the base volume and opens a long against the move.
- **Short entry**: The candle closes above the upper band and the position is flat. The order sells the base volume and opens a short against the move.
- **Exit**: A long is closed on the first close at or above the middle line, a short on the first close at or below it. The original strategy has no stop loss or take profit; its pause of five hundred candles and its limit of three hundred candles per position are not carried over, and since the pause was longer than the limit, every trade in the source actually ended on the time limit and the middle line exit never ran.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Bollinger Period | 20 | Averaging length of the Bollinger Bands. |
| Bollinger Width | 2 | Band width in standard deviations. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame; the original strategy used one-minute candles, the diagram works on five-minute ones. |

## Diagram Details

- The candle block feeds the indicator and a converter for the close price; three more converters read the bands and the middle line out of the indicator value.
- Four comparison blocks turn the close into signals: outside the lower band, outside the upper band, back at the middle from below and back at the middle from above.
- The position block feeds three comparisons against zero, which guard the two entries and the two exits.
- The entry blocks work with the open-position condition and share one volume constant; the exit blocks use the close-position condition and take their volume from the position itself.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
