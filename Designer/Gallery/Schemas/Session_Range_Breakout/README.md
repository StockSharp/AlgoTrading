# Session Range Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades BTCUSDT breakouts of a rolling eight-hour range during the UTC daytime session. Finished hourly candles define the levels and decisions, one shared daily flag admits the first directional candidate, and dedicated evening paths return the schema-created position to flat.

![schema](schema.svg)

## Strategy Overview

- Each finished hourly candle is shifted by one period before entering formed-only Highest 8 and Lowest 8 indicators. On every new candle, the two levels therefore represent the immediately preceding eight completed hours and continue to roll rather than remaining fixed for the day.
- A common Time clock activates the daily reset path from 00:00:00 through 07:59:59 UTC. Candle opening time drives the separate trade window from 08:00:00 through 19:59:59 and close window from 20:00:00 through 23:59:59; together these settings implement the half-open intervals `[08:00, 20:00)` and `[20:00, 24:00)`.
- Inside the trade window, a strict `Close > High` with `Position <= 0` forms the long candidate, while a strict `Close < Low` with `Position >= 0` forms the short candidate. Equality with either range boundary does not trigger an entry.
- Both directional candidates share one Flag, so only the first eligible long or short candidate can enter during a UTC day. Entry quantity is `Base Volume + abs(Position)`: it opens one unit from flat or flattens and reverses an existing one-unit position in a single market order.
- During the close window, a positive position sends one base-volume market sell and a negative position sends one base-volume market buy. There are no stop-loss or take-profit blocks; the chart shows the candles, rolling Highest and Lowest levels, and all four MyTrade streams.

## Entry and Exit Rules

- **Long entry**: From 08:00:00 through 19:59:59 UTC, when a finished candle has `Close > Highest(8)` over the preceding eight hours, the position snapshot is `<= 0`, and the shared daily Flag is available, the diagram submits a NoCondition market buy for `1 + abs(Position)`.
- **Short entry**: From 08:00:00 through 19:59:59 UTC, when a finished candle has `Close < Lowest(8)` over the preceding eight hours, the position snapshot is `>= 0`, and the shared daily Flag is available, the diagram submits a NoCondition market sell for `1 + abs(Position)`.
- **Exit**: From 20:00:00 through 23:59:59 UTC, the diagram sells Base Volume 1 when the position is positive and buys Base Volume 1 when it is negative. In normal operation, entries create exposure of exactly `+1` or `-1`, so this fixed exit quantity returns it to zero. No stop-loss, take-profit, or other protection is attached.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 01:00:00 | One-hour time frame for BTCUSDT; only finished candles feed the rolling range, session checks, position snapshots, and decisions. |
| Range Length | 8 | Number of shifted finished candles used by both formed-only Highest and Lowest indicators; the current candle is excluded. |
| Reset Window | 00:00:00–07:59:59 UTC | UTC interval in which the common Time clock resets the shared once-per-day Flag before the trading session. |
| Trade Window | 08:00:00–19:59:59 UTC | Inclusive configured UTC boundaries for entry candidates, equivalent to the half-open interval `[08:00, 20:00)` by hourly candle opening time. |
| Close Window | 20:00:00–23:59:59 UTC | Inclusive configured UTC boundaries for flattening, equivalent to the half-open interval `[20:00, 24:00)` by hourly candle opening time. |
| Base Volume | 1 | Unit quantity used from flat, added to `abs(Position)` for reversals, and supplied unchanged to both evening exit orders. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits finished one-hour BTCUSDT candles. A [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) block with Shift 1 excludes the decision candle from the range calculation.
- Two formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks calculate Highest 8 and Lowest 8 from the shifted candle stream. Their outputs update on every completed hour and describe the rolling previous-eight-hour channel.
- A common Time stream drives the reset [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) block for 00:00:00–07:59:59 UTC. The candle stream itself drives the trade and close Working time blocks, so those decisions use each candle's OpenTime.
- The current [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) is sampled for every decision candle. [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) and [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) blocks combine strict breakout, session, position-side, and shared-flag checks.
- Entry-volume arithmetic calculates `Base Volume + abs(Position)`. The two entry [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks place NoCondition market orders, making a one-unit long or short from flat and performing a full one-order reversal from the opposite one-unit side.
- The reset window restores one shared Flag for the UTC day, and the first accepted long or short candidate consumes it. In the close window, separate positive- and negative-position paths send fixed Base Volume 1 market orders; this closes the `±1` exposure produced by the schema's normal entry path.
- The [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) receives finished candles, Highest 8, Lowest 8, and MyTrade outputs from the long entry, short entry, long exit, and short exit blocks. No stop-loss or take-profit element is present.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
