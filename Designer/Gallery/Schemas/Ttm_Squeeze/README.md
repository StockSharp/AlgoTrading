# TTM Squeeze Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Quiet markets do not stay quiet. This diagram measures the Bollinger band width as a percentage of the middle band, calls the market squeezed while that width sits below its own moving average, and buys or sells the first candle on which the bands start to open again. The RSI decides which way the breakout is taken.

![schema](schema.svg)

## Strategy Overview

- Band width = (upper band - lower band) / middle band * 100, so the squeeze reading does not depend on the price level of the instrument.
- A simple moving average of that width, multiplied by the squeeze factor, is the line below which the market counts as compressed.
- The trade is taken on expansion, not on compression: the previous candle must have been inside the squeeze and the current width must be larger than it.
- The RSI against its midline gives the direction, and the opposite Bollinger band is where the trade is given up.

## Entry and Exit Rules

- **Long entry**: The band width is above its value on the previous candle, that previous value was at or below the squeeze level, the RSI is above 50, and the position is flat. The buy order opens a long of one lot.
- **Short entry**: The band width is above its value on the previous candle, that previous value was at or below the squeeze level, the RSI is below 50, and the position is flat. The sell order opens a short of one lot.
- **Exit**: A long is closed when the close drops below the lower Bollinger band, a short when the close climbs above the upper band: the breakout failed and went the other way. Both exits run in close-position mode; the original strategy has no stop-loss or take-profit either.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Bollinger Period | 20 | Averaging period of the Bollinger Bands. |
| Bollinger Width | 2 | Band width of the Bollinger Bands, in standard deviations. |
| RSI Length | 14 | Averaging period of the RSI that confirms the direction. |
| Width Average Length | 20 | Length of the moving average taken over the band width itself. |
| Squeeze Factor | 0.9 | Share of that average below which the market counts as squeezed; lower it for rarer, tighter setups. |
| RSI Midline | 50 | RSI level that separates a bullish reading from a bearish one. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:30:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The Bollinger block is read three times by converter blocks: upper band, lower band and middle band; a fourth converter takes the close price of the candle.
- A formula block turns the three bands into the width percentage, which then feeds both a moving average block and a previous-value block, so the diagram compares the width with its own past.
- A second formula multiplies the average width by the squeeze factor, and two comparisons produce the squeeze and the expansion flags.
- Each entry is a four-way logical AND of expansion, squeeze, RSI direction and a flat position; both entry blocks take their volume from the same constant.
- The original strategy also keeps a running minimum of the width, counts three narrow bars, filters the direction with an EMA(20) and pauses fifteen bars after every trade; the diagram replaces the running minimum with the moving average of the width and drops the counter, the EMA and the pause, which no block can express.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
