# EMA Cross Pending Ladder Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades confirmed EMA crossings with a two-rung entry. A market order opens or reverses the position, a farther same-side limit order is placed from the latest BestBid after the market order is fully matched, and absolute-price protection manages the resulting exposure. A 100-candle cooldown suspends both new first-rung entries and finished-close price checks by the protection block.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed formed-only fast EMA 14 and slow EMA 50 values. Crossing produces an upward event when the fast EMA moves above the slow EMA and a downward event when it moves below.
- A candle-time position snapshot and the ready state gate entries. An upward crossing may buy only when Position <= 0, while a downward crossing may sell only when Position >= 0; the market first rung uses Base Volume + abs(Position), so it opens from flat or fully reverses opposite exposure.
- After the first-rung market order reaches Matched, the latest continuously sampled BestBid anchors the second rung. The long branch submits a buy limit at BestBid - 100 and the short branch a sell limit at BestBid + 100, each for Base Volume 1 with ShrinkPrice disabled.
- The most recently registered second-rung order is canceled by an opposite raw EMA crossing, a protective fill, or completion of the cooldown. A second-rung fill is included in position protection but does not restart the cooldown.
- Fills from all four entry-order blocks feed absolute-price protection with Take Distance 400 and Stop Distance 200. While the cooldown is ready, each finished close is checked and a triggered exit is submitted at market. A first-rung fill or protective exit starts the cooldown: the next 100 finished candles allow neither a new first-rung entry nor a protection price check; both resume on the 101st. The chart displays candles, both EMAs, two limit-order streams, and five trade streams.

## Entry and Exit Rules

- **Long entry**: On an upward fast-over-slow EMA crossing, if the position snapshot is less than or equal to zero and the cooldown is ready, the diagram buys Base Volume + abs(Position) at market. Once that order is fully matched, it places a buy limit for Base Volume at the stored BestBid minus Rung Distance.
- **Short entry**: On a downward fast-under-slow EMA crossing, if the position snapshot is greater than or equal to zero and the cooldown is ready, the diagram sells Base Volume + abs(Position) at market. Once that order is fully matched, it places a sell limit for Base Volume at the stored BestBid plus Rung Distance.
- **Exit**: Position protection receives fills from both market rungs and both limit rungs. When the cooldown is ready, finished candle closes are checked and the protected position exits at market if price reaches the favorable 400-unit take distance or adverse 200-unit stop distance. No protection price checks occur during the next 100 finished candles after a first-rung fill or protective exit; checks resume on the 101st. A protective fill cancels the last pending second rung, while an opposite raw crossing also cancels that order independently of entry readiness.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Five-minute time frame; only finished candles drive EMA calculations, signals, protection checks, and cooldown counting. |
| Fast EMA Length | 14 | Length of the fast ExponentialMovingAverage; only formed values are emitted. |
| Slow EMA Length | 50 | Length of the slow ExponentialMovingAverage; only formed values are emitted. |
| Base Volume | 1 | Quantity added to abs(Position) for the market first rung and used without adjustment for the limit second rung. |
| Rung Distance | 100 price units | Absolute price offset from the stored BestBid: subtracted for the buy limit and added for the sell limit. |
| Cooldown | 100 candles | Number of subsequent finished candles on which both new first-rung entries and protection price checks are blocked; both resume on the 101st candle. |
| Take Distance | 400 price units | Absolute favorable price movement that triggers the protective market exit. |
| Stop Distance | 200 price units | Absolute adverse price movement that triggers the protective market exit. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits only finished five-minute candles. Two formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks calculate ExponentialMovingAverage 14 and 50.
- [Crossing](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) emits true upward and false downward events; a NOT [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) makes the downward event actionable. [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) is sampled before the EMA path, and [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks combine Position <= 0 or Position >= 0 with the ready state. The Long and Short entry gates pass only true pulses to the first-rung triggers.
- A [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) calculates Base Volume + abs(Position). The first-rung [Order registration](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) blocks submit NoCondition market orders, and their Matched outputs trigger the corresponding second rung.
- A continuously running [Level1](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html) block supplies BestBid, retained by a [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html). Price [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) blocks calculate BestBid - Rung Distance and BestBid + Rung Distance; the second-rung [Order registration](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) blocks place same-side limit orders with Base Volume and ShrinkPrice false.
- Each new second rung becomes the order held for [Order cancellation](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html). Its cancellation is triggered by the raw crossing toward the other side, a protective fill, or cooldown completion. A limit-rung fill joins the protected exposure without triggering the cooldown.
- [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) consumes fills from the four entry blocks and uses absolute take and stop distances before submitting its market exit. Its stored finished-candle close is released for a price check only while the ready state is active. An [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) block and state variables suppress both first-rung entries and those price checks for exactly 100 following finished candles after a market first-rung fill or protective exit, then restore both for candle 101.
- The [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) receives finished candles, fast EMA 14, slow EMA 50, the buy-limit and sell-limit Order streams, and five MyTrade streams: market buy, market sell, limit buy, limit sell, and protective exit.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
