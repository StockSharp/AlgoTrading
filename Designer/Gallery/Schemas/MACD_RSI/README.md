# MACD + RSI Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

MACD gives the direction and RSI gives the moment. While the MACD line stands above its signal line the diagram waits for the Relative Strength Index to fall into the oversold zone and buys that dip; the mirror rule sells an overbought RSI while MACD is below its signal line. The position is handed back as soon as the two MACD lines swap places.

![schema](schema.svg)

## Strategy Overview

- The trend test is a level comparison, not a crossing: what matters is on which side of the signal line the MACD line currently sits, so the filter stays on for as long as the trend lasts.
- The entry is deliberately contrarian inside that trend - RSI has to be stretched against it, which turns the diagram into a pullback buyer rather than a breakout chaser.
- The exit uses the same pair of lines: a long is closed when MACD drops below its signal line, a short when it climbs above.
- There is no stop loss and no take profit in the diagram, exactly as in the original strategy, where the MACD flip is the only way out.

## Entry and Exit Rules

- **Long entry**: The MACD line is above its signal line, RSI is below the oversold level, and the position is flat. The order buys one lot at market.
- **Short entry**: The MACD line is below its signal line, RSI is above the overbought level, and the position is flat. The order sells one lot at market.
- **Exit**: A long is closed on the first candle where MACD falls below its signal line, a short on the first candle where MACD rises above it; the two closing blocks read the volume off the open position.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| MACD Fast Length | 12 | Length of the fast EMA inside MACD. |
| MACD Slow Length | 26 | Length of the slow EMA inside MACD. |
| MACD Signal Length | 9 | Length of the EMA that smooths MACD into the signal line. |
| RSI Length | 14 | Averaging length of the Relative Strength Index. |
| RSI Oversold | 30 | Level below which RSI is treated as oversold and a long is allowed. |
| RSI Overbought | 70 | Level above which RSI is treated as overbought and a short is allowed. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- One indicator block holds MACD with its signal line; two converter blocks pull the Macd and Signal values out of it, and a second indicator block calculates the Relative Strength Index on the same candles.
- Two comparisons place the MACD line against the signal line, two more place RSI against the threshold constants, and one compares the position with zero.
- Each logical AND joins a trend condition, an RSI condition and the flat-position check, then triggers a position modify block that opens only from flat.
- The trend comparisons are reused as exit triggers, so the two close-position blocks need no extra logic. The 150-bar pause between trades of the original strategy has no counterpart among the blocks and is left out, which makes re-entries more frequent than in the code.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
