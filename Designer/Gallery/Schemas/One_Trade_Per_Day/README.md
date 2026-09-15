# One Trade per 24 Hours Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades strict EMA(10)/EMA(30) crossovers on finished four-hour candles in the reverse direction. A configurable working-time gate controls when candidates are eligible, while a Flag and a six-candle N values counter admit at most one entry decision during each rolling 24-hour interval.

![schema](schema.svg)

## Strategy Overview

- Finished four-hour candles feed EMA 10 and EMA 30. Crossover state develops from the start, but entry candidates are enabled only after ten finished candles.
- A strict upward crossover of the fast EMA through the slow EMA creates a sell candidate. A strict downward crossover creates a buy candidate.
- Time and Working time admit candidates only inside the configured interval. Combination merges the two directional candidate streams, and Flag releases only the first accepted candidate until it is reset.
- The accepted entry starts N values. After six subsequent finished four-hour candles, the counter resets the Flag, creating a rolling 24-hour throttle.
- From flat, Position modify submits one fixed-Volume market order. Against the opposite unit position, it first closes that position and then opens the new side with a second fixed-Volume market order.
- Position protection is the exit mechanism. It tracks direct entry and reversal fills and can close the position at a 3% take-profit or a 2% fixed stop-loss.

## Entry and Exit Rules

- **Long entry**: After a strict downward EMA crossover, ten finished warmup candles, an open Working time gate and an available rolling latch, buy Volume. From flat this opens a long; from a unit short it buys once to close and once more to open the long.
- **Short entry**: After a strict upward EMA crossover, ten finished warmup candles, an open Working time gate and an available rolling latch, sell Volume. From flat this opens a short; from a unit long it sells once to close and once more to open the short.
- **Exit**: Position protection closes the tracked exposure at a 3% take-profit or a 2% fixed stop-loss. A later eligible opposite crossover may instead reverse the position through a close action followed by a new open action.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 04:00:00 | Four-hour time frame; only finished candles drive EMA calculations, warmup, the rolling counter and protection price checks. |
| Fast EMA Length | 10 | Length of the fast exponential moving average. |
| Slow EMA Length | 30 | Length of the slow exponential moving average. |
| Warmup Bars | 10 | Number of finished candles required before crossover candidates may enter. |
| Session From | 00:00:00 | Beginning of the eligible session in strategy replay or server time. |
| Session Until | 23:59:59 | End of the eligible session in strategy replay or server time. |
| Rolling Cooldown Bars | 6 | Number of finished candles counted after an accepted entry before another entry decision is allowed; six four-hour candles equal 24 hours. |
| Volume | 1 | Fixed quantity used by every open or reversal action. |
| Take Profit % | 3 | Favourable percentage move used by Position protection. |
| Stop Loss % | 2 | Adverse percentage move used by the fixed, non-trailing stop. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits finished four-hour candles to two [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks. Crossing identifies changes between EMA 10 and EMA 30; previous/current strict comparisons validate each event, and a NOT branch creates the down-cross pulse.
- A ten-bar warmup gate blocks early candidates. [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/time.html) supplies replay or server time to [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/working_time.html); the packaged-history replay uses UTC.
- Combination passes actionable buy and sell candidates into [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html). The first true candidate is released, starts [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/n_values.html), and locks later candidates until six more finished candles reset the Flag.
- Current position selects the [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) path. A flat entry uses one fixed-side market action; a reversal uses two sequential fixed-side market actions, first to flatten and then to open. Thus the throttle limits accepted entry decisions, while one decision can intentionally create two fills.
- All direct open and reversal fills update [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). Finished candle closes drive its price checks; its own closing fill is not fed back into its trade input.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
