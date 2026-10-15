# Envelope Band Ladder Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades five-minute Bollinger-band mean reversion with a two-rung entry ladder, bounded reversals, middle-band exits, and scheduled cancellation of resting orders.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed Bollinger Bands with Length 20 and Width 1.5. Strict comparisons detect a close below the lower band or above the upper band only after the indicator is formed.
- Entries are accepted from 00:00:00 through 16:59:59 UTC. From flat, the first rung is a one-unit market order and the second is a resting one-unit limit at `2 × lower − middle` for a buy or `2 × upper − middle` for a sell.
- A signal against an existing one- or two-rung position cancels the stale limit and submits a three-unit market order. That maps either permitted exposure to one or two units in the new direction without placing another far rung.
- A return through the middle band has lower priority than a simultaneous opposite-band entry. The exit cancels the resting rung and chains two one-unit ReduceOnly market actions, so the second action executes only when a second unit remains.
- Outside the entry window, a daily Flag activates both mass and targeted cancellation. Open exposure is not closed by time; middle-band exits remain active throughout the day.

## Entry and Exit Rules

- **Long entry**: During the UTC entry window, `Close < lower band` and `Position ≤ 0` form a buy candidate. From flat it submits a one-unit market buy plus a deeper one-unit limit at `2 × lower − middle`; from a short it cancels stale limits and buys three units to reverse the bounded position.
- **Short entry**: During the UTC entry window, `Close > upper band` and `Position ≥ 0` form a sell candidate. From flat it submits a one-unit market sell plus a higher one-unit limit at `2 × upper − middle`; from a long it cancels stale limits and sells three units to reverse the bounded position.
- **Exit**: When no higher-priority opposite entry exists, a long exits after `Close > middle band` and a short exits after `Close < middle band`. Pending rungs are cancelled first; two chained one-unit ReduceOnly market actions remove up to two filled rungs without crossing through flat. The time filter cancels orders but does not force a position exit.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the finished candles used by every signal calculation. |
| Bollinger Length | 20 | Lookback period of the formed-only Bollinger Bands indicator. |
| Bollinger Width | 1.5 | Number of standard deviations used for the upper and lower bands. |
| Entry Start | 00:00:00 UTC | Inclusive UTC start of the fixed entry window. |
| Entry End | 16:59:59 UTC | Inclusive UTC end of the fixed entry window; resting limits are cancelled afterward. |
| Rung Volume | 1 | Quantity of each ordinary ladder rung and each ReduceOnly exit step. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits finished five-minute candles and can build them from packaged minute history. An [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) block calculates the three Bollinger lines.
- [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html), [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html), and [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) blocks extract Close and enforce the strict band, position, session, and priority gates.
- The [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) block is a fixed schema filter. Formula and Variable blocks calculate and snapshot both far-rung prices and the three-rung reversal quantity at the instant of entry.
- Six [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) blocks cover flat market entries, bounded market reversals, and the two resting limits. [Mass order cancellation](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html) blocks mark every cancellation boundary, while two targeted Order cancellation blocks retain and cancel the active far rungs.
- Two [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks perform the sequenced ReduceOnly exit. The [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) displays candles, all three bands, and the Strategy trades stream.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
