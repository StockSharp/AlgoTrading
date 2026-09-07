# Early Bird Range Latch Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades a strict breakout of the preceding five-minute candle when price agrees with EMA 20. A UTC-day latch accepts at most one new position per day, while current ATR 14 defines the stop and target boundaries.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles supply the current close, the previous candle's high and low, EMA 20, and ATR 14. The Previous value blocks shift only the high and low streams, so the decision never compares a candle with its own extremes.
- The long setup requires `Close > previous High` and `Close > EMA 20`; the short setup requires `Close < previous Low` and `Close < EMA 20`. Every comparison is strict, so equality does not qualify.
- A Time block drives the fixed daily-reset interval from 00:00:00 through 00:04:59 UTC. Candle time drives the fixed entry interval from 00:05:00 through 23:59:59, and one shared Flag passes only the first eligible directional setup after each reset.
- An accepted setup captures the current close as the entry price and opens one market unit only while the position snapshot is zero. The latch remains consumed after the position exits, preventing another entry until the next UTC reset.
- On every later finished candle, formulas recalculate four boundaries from the captured entry and current ATR: long stop/target at `entry − 1.5×ATR` and `entry + 2.5×ATR`, with the signs reversed for a short. ReduceOnly market actions close the matching side when either boundary is reached.

## Entry and Exit Rules

- **Long entry**: After EMA 20 is formed, from 00:05:00 through 23:59:59 UTC, a flat position, `Close > previous High`, `Close > EMA 20`, and an available daily Flag submit an OpenPosition market buy for one unit.
- **Short entry**: After EMA 20 is formed, from 00:05:00 through 23:59:59 UTC, a flat position, `Close < previous Low`, `Close < EMA 20`, and an available daily Flag submit an OpenPosition market sell for one unit.
- **Exit**: For a long, a ReduceOnly market sell fires at `Close ≤ entry − 1.5×current ATR` or `Close ≥ entry + 2.5×current ATR`. For a short, a ReduceOnly market buy fires at `Close ≥ entry + 1.5×current ATR` or `Close ≤ entry − 2.5×current ATR`. There is no time-based exit, trailing rule, reversal, or same-day re-entry.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the finished candles that drive all signal and risk calculations. |
| EMA Length | 20 | Period of the formed-only exponential moving average used as the directional filter. |
| ATR Length | 14 | Period of the formed-only Average True Range recalculated for every finished candle. |
| Stop ATR Multiplier | 1.5 | Multiplier applied to current ATR when placing the adverse boundary around the captured entry price. |
| Target ATR Multiplier | 2.5 | Multiplier applied to current ATR when placing the favourable boundary around the captured entry price. |
| Order Volume | 1 | Fixed quantity supplied to both OpenPosition entries and both ReduceOnly exits. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits finished five-minute candles and can build them from the packaged minute history.
- Three candle [Converters](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) extract Close, High, and Low. Two [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) blocks apply Shift 1 to the numeric High and Low streams.
- Formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks calculate EMA 20 for direction and ATR 14 for risk distance. EMA readiness also prevents entries before both indicators have enough data.
- The [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/time.html) stream feeds the reset [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) block. A separate Working time block reads candle time and participates directly in both entry conditions.
- A shared [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) consumes the first long or short candidate of the UTC day. Variable blocks snapshot both the position and the accepted entry close; a second entry-price variable republishes the stored value on each candle for the risk formulas.
- [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html), [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html), and [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) blocks build the breakout gates and all four ATR boundaries. Per-candle exit Flags prevent duplicate close actions if several inputs update during one evaluation.
- Two OpenPosition and two ReduceOnly [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks handle market entries and exits. The [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) receives candles, previous High and Low, EMA, ATR, and a Combination stream containing every fill.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
