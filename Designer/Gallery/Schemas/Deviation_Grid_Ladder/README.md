# EMA Deviation Grid Ladder Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram combines the C# strategy's EMA(30)/StandardDeviation(14) mean-reversion signal with the adjacent README's grid idea. A 1.5σ deviation arms a marketable near limit and a pending 2.5σ averaging rung; the position exits on the source's return boundary at EMA ± 0.5σ.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute replay candles update EMA(30), StandardDeviation(14), and a synchronized close used after both indicators are current.
- A downward crossing of EMA − 1.5σ registers buy limits at −1.5σ and −2.5σ when Position <= 0; the upper side is symmetric for Position >= 0.
- The near rung is already through the market when the crossing is confirmed and normally fills immediately; the far rung remains pending for a stronger move.
- A long closes when Close > EMA + 0.5σ and a short closes when Close < EMA − 0.5σ, exactly matching the code's mean-reversion exits.
- Every mean-reversion exit cancels both far orders, while an opposing setup removes the stale far rung from the previous side.

## Entry and Exit Rules

- **Long entry**: When close crosses below EMA − 1.5σ and Position <= 0, register one-unit buy limits at EMA − 1.5σ and EMA − 2.5σ.
- **Short entry**: When close crosses above EMA + 1.5σ and Position >= 0, register one-unit sell limits at EMA + 1.5σ and EMA + 2.5σ.
- **Exit**: For a long, Close > EMA + 0.5σ triggers market ClosePosition; for a short, Close < EMA − 0.5σ does the same. ClosePosition automatically sizes the order to the whole current position, including two filled rungs.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 00:05:00 | Finished-candle interval; five minutes is a replay adaptation from the C# four-hour default. |
| EMA Length | 30 | Center-line EMA period and one of the two true C# strategy parameters. |
| Standard Deviation Length | 14 | Width-estimator period; the value 14 is a literal in the C# constructor path. |
| Near Entry Deviation | 1.5σ | First entry multiplier; 1.5 is a literal in the source trading condition. |
| Far Grid Deviation | 2.5σ | Second pending grid rank added from the README, not present in executable C# logic. |
| Mean-Reversion Exit Deviation | 0.5σ | Return boundary used by the source exits; 0.5 is a C# literal. |
| Volume per Rung | 1 | Independent limit-order size for each near and far rung. |

## Diagram Details

- Only EmaLength and CandleType are StrategyParam values in C#. StandardDeviation length 14 and multipliers 1.5 and 0.5 are literals; the diagram exposes them for study without claiming they are source parameters.
- The C# default is four-hour candles. Five-minute candles are a replay adaptation that supplies enough formed indicators and deviation events during the one-month acceptance interval.
- The executable C# contains one 1.5σ entry threshold despite the folder name Three Level Grid. The second 2.5σ rung is explicitly an execution rank borrowed from the README, not hidden source behavior.
- The source reverses an opposite position with two immediate market orders. The diagram preserves Position <= 0 / >= 0 gates but simplifies execution: the near limit can flatten the old position and the far pending rung may complete a later reversal.
- Crossing blocks create one ladder per boundary excursion. Order cancellation is essential: an averaging rung left after a mean-reversion exit could otherwise open an unmanaged position later.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
