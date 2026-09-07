# Three Red Candles with Timed Exit Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades three consecutive candles of one colour when volatility is elevated. Three red candles with ATR 14 above 0.8 times its 30-value average produce a long setup; three green candles under the same volatility rule produce a short setup. A flat position opens directly, an opposite position is reversed through a fill-confirmed two-step sequence, and an existing position can close on the opposite three-candle pattern or after twenty finished bars. Every action sequence starts a twelve-candle cooldown.

![schema](schema.svg)

## Strategy Overview

- Only finished 30-minute candles are processed. Two Previous value blocks and six Converter blocks expose Open and Close for the current candle and the preceding two candles, so every bar receives a rolling three-candle red and green test.
- ATR 14 and its 30-value simple average must both be formed. High volatility is the strict condition `ATR > ATR average × 0.8`; no trading decision is made during indicator warm-up.
- A qualifying three-red setup buys from flat or reverses a short. A qualifying three-green setup sells from flat or reverses a long. Reversals close one unit with ReduceOnly, then open one unit in the new direction only after the close Order is fully matched.
- Without a qualifying high-volatility reversal, three green candles close a long and three red candles close a short. A stateful counter also closes either side when the position has been held for twenty finished bars; a same-candle reversal has priority over that timed exit.
- Three Combination blocks merge the two long-exit reasons, the two short-exit reasons, and all eight action-fill streams. A fill disables further actions for the next twelve finished candles; the thirteenth candle is the first eligible one. There is no stop-loss, take-profit, or position-protection block.

## Entry and Exit Rules

- **Long entry**: When the current and previous two finished candles all close below their opens, ATR 14 is strictly above `ATR average × 0.8`, and the cooldown is ready, a flat position submits a NoCondition market buy for Order Volume 1. From a short position, the diagram first submits a ReduceOnly market buy for 1; its fully matched Order refreshes the volume value and triggers the NoCondition market buy for 1.
- **Short entry**: When the current and previous two finished candles all close above their opens, ATR 14 is strictly above `ATR average × 0.8`, and the cooldown is ready, a flat position submits a NoCondition market sell for Order Volume 1. From a long position, the diagram first submits a ReduceOnly market sell for 1; its fully matched Order refreshes the volume value and triggers the NoCondition market sell for 1.
- **Exit**: A long closes on three consecutive green candles when that pattern is not simultaneously a high-volatility reversal, or when Max Hold Bars reaches 20. A short closes symmetrically on three red candles or after 20 bars. Pattern and timer events are merged per side, and a Flag permits at most one standalone close per candle. Every standalone close and both fills of a staged reversal feed the common cooldown stream.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles Series | 00:30:00 | Thirty-minute series; only finished candles update patterns, indicators, counters, cooldown state, and decisions. |
| ATR Length | 14 | Period of the formed-only Average True Range indicator. |
| ATR Average Length | 30 | Period of the formed-only simple moving average calculated from ATR values; decisions wait until this average is formed. |
| ATR Multiplier | 0.8 | Multiplier applied to the ATR average. Volatility qualifies only when ATR is strictly greater than the resulting threshold. |
| Max Hold Bars | 20 | Number of finished bars counted while a position is non-flat before the timed close becomes eligible. |
| Cooldown Bars | 12 | Number of subsequent finished candles blocked after an action sequence; decisions resume on candle 13. |
| Order Volume | 1 | Fixed quantity for flat entries, ReduceOnly closes, and entries after a matched reversal close; the diagram is sized for its own one-unit position. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits finished 30-minute candles. Two [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) blocks retain shifts 1 and 2, while six [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) blocks extract the three Open/Close pairs.
- Six [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks classify each candle with strict `Close < Open` and `Close > Open` tests. Two three-input [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) blocks form the rolling red and green patterns; a doji makes both patterns false.
- Formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks calculate ATR 14 and SMA 30 of ATR. A [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) block multiplies the average by 0.8, and a strict comparison supplies the high-volatility flag.
- The current [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) is captured twice per candle: once for trade routing and once for the hold counter. [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) and Formula blocks increment the counter only while non-flat and reset it from confirmed action fills.
- Two Boolean [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) blocks merge pattern and timer exits without counting or changing them. Per-side [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) blocks prevent duplicate standalone closes, and priority gates suppress a timed or pattern close when the same candle already qualifies for reversal.
- Eight [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks implement two flat entries, two standalone exits, and two staged reversals. Each reversal uses `ReduceOnly close 1 → fully matched Order → NoCondition open 1`; the matched Order also re-emits volume in the same event cycle.
- A MyTrade Combination sends every action fill to an [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) cooldown. The first fill starts its twelve-candle count, while the second leg of the same reversal cannot restart an active count. The [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) receives candles, ATR, its average, the threshold, and all action fills.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
