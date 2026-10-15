# MFI Limit Entry with Timed Cancellation
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This long-only diagram shows one pending order's complete lifecycle. MFI(14) leaving the oversold zone registers a buy below the close, N values counts five finished candles, Order cancellation removes an unfilled limit, and fills are routed through the legacy Trades for order block into 1%/1% position protection.

![schema](schema.svg)

## Strategy Overview

- An upward crossing of MFI through 20 models the source's remembered visit to the oversold zone.
- While Position == 0, the event registers one buy limit at Close × (1 − 0.5/100) with volume one.
- A one-lifecycle flag starts the five-candle timer and refuses another order until that timer completes, so an old timeout cannot cancel a newer order.
- If the order fills, Trades for order forwards its own trades to take-profit and stop-loss protection; otherwise the timer cancels the exact registered order.

## Entry and Exit Rules

- **Long entry**: MFI crosses upward out of the zone below 20 while the position is flat. One buy limit is placed 0.5% below the finished close.
- **Short entry**: There is no short entry, matching the C# strategy.
- **Exit**: A filled entry closes at +1% or −1%. An unfilled entry is cancelled after five finished candles, counting the signal candle as the first just as the source increments its order age immediately.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 00:05:00 | Finished-candle interval used by MFI, pricing, and the cancellation counter. |
| MFI Period | 14 | Number of candle values used by MoneyFlowIndex. |
| MFI Oversold Level | 20 | MFI level whose upward crossing arms the long entry. |
| Replay Entry Offset, % | 0.5 | Diagram limit distance below close; the C# default is 0.1%, while 0.5% exposes cancellation in replay. |
| Order Volume | 1 | Volume of the single pending buy limit. |
| Cancel After Candles | 5 | Finished candles counted from registration before cancellation is attempted. |
| Take Profit, % | 1 | Percentage gain from the fill used by Position protection. |
| Stop Loss, % | 1 | Percentage loss from the fill used by Position protection. |

## Diagram Details

- The C# default entry offset is 0.1%. Candle matching fills a limit whenever its price lies inside Low..High, so that distance almost always executes immediately on five-minute BTCUSDT. The diagram uses 0.5% to make timed cancellation observable and states the source value here.
- Trades for order is obsolete; Designer recommends the Trades output of Order registering. It is used exactly once in this gallery because the legacy block itself is the lesson, and should not be copied into new diagrams.
- The one-lifecycle flag is released by the timer, not by a fill. Consequently a still-running old timer cannot later see and cancel a replacement order through the Order socket.
- The source requires a 20-bar cooldown between signals. This compact diagram omits that separate cooldown; the five-bar lifecycle lock still prevents overlapping pending entries.
- Despite the source folder name, the executable C# contains no averaging orders. The diagram likewise has one entry order and no Chart panel, as requested by the reviewed blueprint.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
