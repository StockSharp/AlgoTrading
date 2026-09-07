# Scheduled Hour SMA Cross
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram evaluates a fast and a slow moving average on finished thirty-minute BTCUSDT candles, permits scheduled entries when the candle opening hour is 12, closes contrary positions outside that hour, and spaces market actions with an eight-bar cooldown.

![schema](schema.svg)

## Strategy Overview

- Finished thirty-minute candles feed formed-only SMA(8) and SMA(21). No decision is released until both values exist for the same candle.
- A bullish trend means `SMA(8) > SMA(21)`; a bearish trend means `SMA(8) < SMA(21)`. Equal values produce no action.
- The Time block supplies the decision timestamp and Converter extracts its Hour. In each finished-candle batch that timestamp aligns with the candle `OpenTime` used by the rule. A one-shot Flag allows exactly one decision for that candle, including during synchronous order events.
- A fill-driven state records the net position as `-1`, `0`, or `1`. Each of the four action paths prepares its own next state before submitting an order and commits it only when that action reports a fill.
- Every action starts an eight-bar cooldown. Subsequent candles 1 through 7 remain blocked; the eighth subsequent finished candle reduces the counter to zero before its decision and is eligible again.

## Entry and Exit Rules

- **Scheduled bullish action**: On a candle whose `OpenTime.Hour` is 12, a bullish trend with a flat or short position sends a market buy of Volume 1. A short position is reduced to flat; it is not reversed.
- **Scheduled bearish action**: In the same hour, a bearish trend with a flat or long position sends a market sell of Volume 1. A long position is reduced to flat; it is not reversed.
- **Outside-hour exit**: At every other opening hour, a bearish trend closes a long with one market sell, while a bullish trend closes a short with one market buy. No new position is opened outside hour 12.
- There is no separate closing hour. All four branches require an available cooldown, and only their true branch can trigger its independent Modify position block.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| BTC Data Security | BTCUSDT@BNBFT | Instrument used by the Candles and Strategy trades blocks. Set Strategy Security to the same instrument for transactions. |
| Candle Series | 00:30:00 | Finished candle interval and the clock for indicator updates and cooldown steps. |
| Fast SMA Length | 8 | Number of finished candles in the fast simple moving average. |
| Slow SMA Length | 21 | Number of finished candles in the slow simple moving average. |
| Trade Hour | 12 | Accepted value of the finished candle's `OpenTime.Hour` field for scheduled entry actions. |
| Cooldown N | 8 | Earliest subsequent finished-candle index at which another action can be considered. |
| Volume | 1 | Fixed quantity of every market buy or sell action. |

## Diagram Details

- The BTC [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) feeds built, finished [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) and [Strategy trades](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html).
- Two formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks calculate SMA(8) and SMA(21). Formula blocks expose numeric values to bullish and bearish [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks.
- Time first releases the staged position, hour constant, zero reference, cooldown state, its timestamp to the Hour Converter, and fixed volume. It triggers the pending-decision latch last, so every five-input logical gate sees one coherent candle snapshot.
- The one-shot Flag suppresses re-entry while synchronous transaction events are being processed. Four separate [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks keep a false branch from consuming the volume staged for a true branch.
- Scheduled buy and sell candidates are `position + 1` and `position - 1`; both exit candidates are zero. A candidate reaches the position state only through the matching Modify position `MyTrade` output.
- The [Delay](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) block receives each candle before same-candle decisions. A true action gate marks cooldown unavailable and arms N = 8 before submitting the order. The chart shows candles, both averages, filled position, four action-fill streams, and all strategy fills.

## Usage

Import the `.json` file into Designer, set Strategy Security to BTCUSDT@BNBFT, choose a portfolio, and run the diagram on thirty-minute history. With the packaged March data and the defaults above, validation produced 59 completed market orders and 59 fills without transaction failures. Confirm the candle time zone, hour field, volume, and cooldown behavior before live trading.
