# EMA + MACD + RSI Trend Combo Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Three independent checks have to agree before this diagram trades. The distance between EMA 50 and EMA 200 says which side is allowed, the MACD line crossing its signal line says when, and RSI has to be inside a middle band - strong enough to confirm the move but not yet stretched. Each accepted signal reverses the position with a single market order.

![schema](schema.svg)

## Strategy Overview

- The trend filter is a level comparison of two exponential averages: nothing is bought while EMA 50 sits below EMA 200, and nothing is sold while it sits above.
- The entry itself is an event, not a state: only the candle on which the MACD line crosses its signal line can open a trade, so the diagram does not keep firing for as long as the trend holds.
- The RSI corridor is what makes the combination careful. A long needs RSI above the buy level and still below the upper bound, a short needs RSI below the sell level and still above the lower bound, so exhausted moves are skipped.
- The original strategy runs on 30-minute candles; the diagram is scaled to five-minute candles to match the packaged sample history. Its pause of ten bars after a trade has no block equivalent and is left out, which makes re-entries somewhat more frequent than in the code.

## Entry and Exit Rules

- **Long entry**: EMA 50 is above EMA 200, the MACD line crosses above its signal line, RSI is above the buy level and still below the upper bound, and the position is not already long. The order buys the base volume plus any open short, so a short is reversed into a long by one market order.
- **Short entry**: EMA 50 is below EMA 200, the MACD line crosses below its signal line, RSI is below the sell level and still above the lower bound, and the position is not already short. The order sells the base volume plus any open long, so a long is reversed into a short by one market order.
- **Exit**: There is no exit block and no protection, exactly as in the original: the position is held until the mirror signal appears, and that same order closes the old trade and opens the new one.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Fast EMA length | 50 | Length of the fast exponential average that carries the short-term trend. |
| Slow EMA length | 200 | Length of the slow exponential average the fast one is measured against. |
| MACD fast length | 12 | Length of the fast EMA inside MACD. |
| MACD slow length | 26 | Length of the slow EMA inside MACD. |
| MACD signal length | 9 | Length of the EMA that smooths MACD into the signal line. |
| RSI length | 14 | Averaging length of the Relative Strength Index. |
| RSI buy level | 40 | RSI has to be above this level for a long to be accepted. |
| RSI sell level | 60 | RSI has to be below this level for a short to be accepted. |
| RSI upper bound | 70 | Upper bound of the RSI corridor; above it a long is treated as too late. |
| RSI lower bound | 30 | Lower bound of the RSI corridor; below it a short is treated as too late. |
| Volume | 1 | Base order volume, in lots; a reversal adds the open position on top of it. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- One candle block feeds four indicator blocks: the two exponential averages, MACD with its signal line and the Relative Strength Index.
- Two converter blocks split the MACD value into the Macd and Signal lines; a crossing block turns that pair into the bullish trigger and a NOT block inverts it into the bearish one.
- Eight comparison blocks build the filters - one pair for the averages, four for the RSI corridor and two for the position against zero.
- Each logical AND joins five conditions before it triggers a position modify block, and a formula block adds the base volume to the absolute value of the position so a single market order performs the whole reversal.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
