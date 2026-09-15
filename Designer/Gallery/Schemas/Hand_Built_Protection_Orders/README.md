# Hand-Built Protection Orders Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram opens or fully reverses a position on confirmed EMA(14)/EMA(50) crossings and builds its protection from explicit order-lifecycle blocks. The accepted entry fill fixes both protection levels. After a 100-candle cooldown, one entry-relative take-profit limit is registered; later replacements keep the same target price, while a stop or eligible reversal first sends a cancellation request for the working take and then triggers the market position change without waiting for cancellation acknowledgement.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed formed-only fast EMA 14 and slow EMA 50 values. Crossing emits the upward event when the fast EMA moves above the slow EMA and the downward event when it moves below.
- An upward crossing may act only when Position <= 0, while a downward crossing may act only when Position >= 0. Position comparisons route a flat signal to one market order of fixed Volume. Against opposite exposure, the reversal route triggers two market orders of that same Volume in sequence: the first closes the existing side and the second opens the new side immediately, without waiting for the first fill.
- Every new fill from Strategy trades resets the counter to 0 and restarts the cooldown. Exactly the next 100 finished candles block new entries, take registration, replacement, and stop checks; processing resumes on candle 101 unless another fill restarts the window.
- When the cooldown has completed, a one-shot flag registers a take of fixed Volume. Order-book readiness is not required for this first registration; it gates only later candle-clocked replacements. A long uses a sell limit at Entry Price * (1 + Take Profit fraction); a short uses a buy limit at Entry Price * (1 - Take Profit fraction).
- Combination only merges the first registration Order and replacement clones into one order stream. The connected replaceOrder.order and cancelOrder.order inputs remember the most recently forwarded order. The order-book branch is deliberately simplified: BestBid and BestAsk only gate later candle-clocked replacements at the same fixed target and never move that target. A stop or eligible reversal first requests take cancellation and then triggers the market action without waiting for acknowledgement.

## Entry and Exit Rules

- **Long entry**: On a confirmed upward EMA crossing, if Position <= 0 and the cooldown is ready, the diagram sends one market buy of Volume from flat. From a short it first requests cancellation of the short take, then triggers two market buys of the same Volume: the first closes the short and the second opens the long immediately without waiting for the closing fill. Entry Price comes only from the flat-opening fill or the second, opening fill of a reversal.
- **Short entry**: On a confirmed downward EMA crossing, if Position >= 0 and the cooldown is ready, the diagram sends one market sell of Volume from flat. From a long it first requests cancellation of the long take, then triggers two market sells of the same Volume: the first closes the long and the second opens the short immediately without waiting for the closing fill. Entry Price comes only from the flat-opening fill or the second, opening fill of a reversal.
- **Exit**: After the cooldown, the long branch keeps one sell limit of fixed Volume at Entry Price * (1 + 0.006), and the short branch keeps one buy limit of fixed Volume at Entry Price * (1 - 0.006). A finished close at or below Entry Price * (1 - 0.003) stops a long; a close at or above Entry Price * (1 + 0.003) stops a short. The stop first requests cancellation of the working take and then sends one opposite market order of the same fixed Volume without waiting for acknowledgement. An eligible opposite crossing uses the same request-then-trigger rule before its two-order reversal.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Five-minute time frame; only finished candles drive EMA signals, cooldown counting, stop checks, and the order-lifecycle clock. |
| Fast EMA Length | 14 | Length of the fast ExponentialMovingAverage; only formed values are emitted. |
| Slow EMA Length | 50 | Length of the slow ExponentialMovingAverage; only formed values are emitted. |
| Take Profit fraction | 0.006 | Favorable fraction measured from Entry Price. The value 0.006 is 0.6% and gives a 1:2 stop-to-take ratio. |
| Stop fraction | 0.003 | Adverse fraction measured from Entry Price. The value 0.003 is 0.3%. |
| Volume | 1 | Fixed quantity used by every entry leg, take registration and replacement, and market stop. A reversal sends two separate market orders of this same Volume: close first, then open. |
| Cooldown | 100 | Number of subsequent finished candles blocked after every Strategy trades fill. Each new fill resets the count to 0; processing resumes on candle 101 if no later fill restarts the window. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits only finished five-minute candles. A close-price converter and two formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks supply the close, ExponentialMovingAverage 14, and ExponentialMovingAverage 50.
- [Crossing](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/crossing.html), [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html), and [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) build the mirrored Position <= 0 and Position >= 0 entry gates.
- Separate [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks implement flat OpenPosition actions and NoCondition close and open legs of reversals. Every block receives the same fixed Volume. The closing leg is triggered first, but the opening leg follows immediately without waiting for its fill, so asynchronous execution can race. Formula blocks calculate cooldown state and take and stop values, never an order quantity.
- [Strategy trades](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) is used only to reset the cooldown on every own fill and to feed the chart. Only the trade output of a flat-opening block and the trade output of the second, opening reversal leg supply the accepted Trade.Price to the corresponding long or short protection state; the first, closing reversal fill is excluded.
- [Market depth](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/market_depth.html) supplies BestBid and BestAsk readiness. Neither quote is part of the take-price formulas: both long and short targets remain anchored to Entry Price.
- Long and short price formulas feed their respective limit [Order registration](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) blocks. The flag permits one registration after the cooldown, and each later replacement reuses both the same calculated price and the same fixed Volume.
- The first registration Order and each replacement clone enter a Combination<Order> bus, which only merges and forwards them. The connected replaceOrder.order inputs and the cancelOrder.order inputs of [Order cancellation](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html) remember the most recently forwarded order. During an in-flight re-registration, cancellation can therefore still target the previously forwarded order reference. On a stop or reversal, the cancellation request is sent first and the fixed-Volume market stop or two fixed-Volume reversal legs are triggered immediately afterward without waiting for cancellation acknowledgement; atomicity is not guaranteed.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
