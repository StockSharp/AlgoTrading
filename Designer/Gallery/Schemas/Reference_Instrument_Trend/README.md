# Reference Instrument Trend Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram first synchronizes finished five-minute BTCUSDT@BNBFT and TONUSDT@BNBFT candles, then trades exact BTC EMA crossovers when the TON EMA trend confirms the same direction. Position and cooldown gates filter each decision, and two fixed-volume market-order paths manage exposure.

![schema](schema.svg)

## Strategy Overview

- The Sync block receives the two finished five-minute candle streams with Interval `00:05:00` and ClearSockets enabled. It emits an aligned BTC–TON pair only when both candles are present; an incomplete interval is discarded when either candle is missing.
- Each aligned pair then feeds BTC fast EMA 7 and slow EMA 18, and TON fast EMA 47 and slow EMA 50. Formed-only filtering is disabled for all four indicators.
- A BTC upward crossover requires `PrevFast <= PrevSlow` and `Fast > Slow`; a downward crossover requires `PrevFast >= PrevSlow` and `Fast < Slow`. The current TON relation confirms buys with `Fast > Slow` and sells with `Fast < Slow`.
- The buy path additionally requires `Position <= 0`, while the sell path requires `Position >= 0`. Both paths submit `NoCondition` market orders with fixed Volume 1.
- The first five synchronized BTC–TON pairs are blocked, and every order signal blocks the next five synchronized pairs; the sixth aligned pair is eligible again. There is no stop-loss, take-profit, or separate exit block, and the chart displays BTC candles, both BTC EMAs, and both fill streams.

## Entry and Exit Rules

- **Long entry**: When BTC satisfies `PrevFast <= PrevSlow` and `Fast > Slow`, TON currently has `Fast > Slow`, the synchronized position check is `Position <= 0`, and the cooldown is ready, the diagram submits a market buy with Volume 1.
- **Short entry**: When BTC satisfies `PrevFast >= PrevSlow` and `Fast < Slow`, TON currently has `Fast < Slow`, the synchronized position check is `Position >= 0`, and the cooldown is ready, the diagram submits a market sell with Volume 1.
- **Exit**: There is no dedicated exit or protection block. A later eligible order in the opposite direction reduces exposure; from a position of `+1` or `-1`, the fixed Volume 1 order brings the position to zero rather than opening the opposite side.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Main Fast EMA | 7 | Period of the fast ExponentialMovingAverage calculated from finished five-minute BTCUSDT@BNBFT candles; formed-only filtering is disabled. |
| Main Slow EMA | 18 | Period of the slow ExponentialMovingAverage calculated from finished five-minute BTCUSDT@BNBFT candles; formed-only filtering is disabled. |
| Reference Fast EMA | 47 | Period of the fast ExponentialMovingAverage calculated from the aligned finished five-minute TONUSDT@BNBFT candle; formed-only filtering is disabled. |
| Reference Slow EMA | 50 | Period of the slow ExponentialMovingAverage calculated from the aligned finished five-minute TONUSDT@BNBFT candle; formed-only filtering is disabled. |
| Cooldown Bars | 5 | Number of initial and post-signal synchronized candle pairs blocked before the next aligned pair becomes eligible. |
| Volume | 1 | Fixed quantity supplied to both NoCondition market-order blocks. |

## Diagram Details

- Two [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) blocks emit finished five-minute candles for BTCUSDT@BNBFT and TONUSDT@BNBFT directly into synchronization.
- The [Sync](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/sync.html) block aligns the two candle inputs with Interval `00:05:00` and ClearSockets `true`. It releases both candles as one pair; if either side is absent, that incomplete interval is cleared without entering the indicator chain.
- Only the synchronized pair feeds four [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks: ExponentialMovingAverage 7 and 18 for BTC and 47 and 50 for TON. Their formed-only option is `false`, while only the two BTC EMA outputs are also sent to the chart.
- [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) blocks retain the prior synchronized BTC fast and slow EMA values. [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks express both sides of each exact crossover and the two current TON trend relations; separate [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) paths combine them for buying and selling.
- The current [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) supplies the `Position <= 0` and `Position >= 0` checks. The [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) gate receives the synchronized BTC candle output and uses N=5 to suppress the first five aligned pairs and the five aligned pairs after each order signal, then re-enables decisions on the sixth.
- The buy and sell [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks place market orders with `NoCondition` and shared Volume 1. No stop-loss, take-profit, position-protection, or separate exit element is present.
- The [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) receives finished BTC candles, BTC EMA 7, BTC EMA 18, and the MyTrade outputs of the buy and sell order blocks.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
