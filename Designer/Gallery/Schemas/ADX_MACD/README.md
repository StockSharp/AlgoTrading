# ADX + MACD Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two classic indicators split the work: MACD against its own signal line says which way the market leans, and ADX says whether the move is strong enough to be worth trading. Entries need both, while the exit listens to MACD alone, so a position is left as soon as the momentum flips even if the trend still measures as strong.

![schema](schema.svg)

## Strategy Overview

- The ADX line of the Average Directional Index is taken from the complex indicator value and compared with a single strength threshold.
- Direction comes from the level of the MACD line relative to its signal line, not from the moment of the crossing, so a fresh position can be opened at any point while MACD stays on one side.
- The strength filter guards entries only: the exit fires purely on the opposite side of MACD, and there is no stop-loss or take-profit in the diagram.

## Entry and Exit Rules

- **Long entry**: ADX is above the threshold, the MACD line is above its signal line and the position is flat. The modify block buys the shared volume at market.
- **Short entry**: ADX is above the threshold, the MACD line is below its signal line and the position is flat. The modify block sells the shared volume at market.
- **Exit**: A long is closed when the MACD line drops below its signal line and a short when it rises above it; the ADX filter is not consulted on the way out.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| ADX Length | 14 | Length of the Average Directional Index, which sets both the directional index and its smoothing. |
| ADX Threshold | 25 | Strength level the ADX line must exceed before an entry is allowed. |
| Fast EMA length | 12 | Length of the fast EMA inside MACD. |
| Slow EMA length | 26 | Length of the slow EMA inside MACD. |
| Signal EMA length | 9 | Length of the signal EMA calculated on the MACD line. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both indicators; converters pull the ADX line out of the Average Directional Index and the MACD and signal lines out of the MACD indicator.
- Three comparisons produce the market conditions — trend strength, MACD above the signal line and MACD below it — and three more compare the position with zero.
- The entry AND blocks join strength, direction and a flat position; the exit AND blocks join direction with an open position of the opposite side.
- The 100-bar pause the C# strategy keeps between trades cannot be built from Designer blocks, so this diagram enters and exits more often than the original.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
