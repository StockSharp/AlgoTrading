# Session Channel Limit Orders Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram takes a fixed daily snapshot of a nine-hour price channel and places a client-managed OCO pair of resting limit orders at its boundaries. Diagram logic requests cancellation of the peer after a fill; this is not an exchange-atomic OCO instruction. Finished five-minute candles define the channel, a live BestBid subscription supplies quote events for execution, and the next daily reset cancels remaining orders before a reduce-only market action adjusts the position by Order Volume.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed formed-only Highest 108 and Lowest 108 indicators. At the daily snapshot, their rolling window contains the candles opened from 01:00 through 09:55 UTC, which is the complete 01:00–10:00 session.
- The placement Working time block selects the finished candle stamped 09:55:00–09:59:59. Candle timestamps carry OpenTime, so that candle is delivered when it completes at 10:00 UTC; a Flag converts the window result into exactly one placement pulse for the session.
- The pulse captures both channel boundaries and the current position. When Session Low < Session High and Position = 0, the diagram registers a buy limit at the captured low and then a sell limit at the captured high, both with Order Volume 1 and ShrinkPrice disabled.
- A continuously subscribed Level1 block reads BestBid. Its quote stream keeps live price updates available to the trading connector so the two resting orders can execute when the market reaches their prices; BestBid does not replace either captured channel boundary.
- The first MyTrade from either limit order requests cancellation of the stored opposite Order. This is client-side OCO logic: under normal sequential event processing it is intended to leave one filled channel order, but near-simultaneous fills can race because cancellation is not atomic at the exchange. At the next reset, the diagram issues a bulk cancellation request, also sends both stored Order references through their deterministic cancellation paths, and submits a reduce-only market action for Order Volume. That quantity closes the normal one-fill position; if actual exposure differs, ReduceOnly only decreases it. The chart shows candles, both channel lines, the two order streams, and all entry and reset-close trades.

## Entry and Exit Rules

- **Long entry**: At the 10:00 UTC session boundary, if both 108-value indicators are formed, the captured low is below the captured high, and the position snapshot is zero, the diagram places a buy limit for Order Volume at Session Low. The order remains active until it fills or a cancellation path removes it.
- **Short entry**: Under the same formed-channel and flat-position checks, the diagram places a sell limit for Order Volume at Session High. If this order produces the first fill, its MyTrade event sends the stored buy limit to the cancellation block.
- **Exit**: There is no stop-loss or take-profit block. After the first channel fill, client-side logic requests cancellation of the opposite order instead of intentionally reversing the position. The position remains open until the reset associated with the 00:55 candle, delivered on completion at 01:00 UTC; reset requests bulk cancellation, explicitly cancels both stored limits, and uses a reduce-only market action for Order Volume. It closes the normal one-fill position and cannot increase or reverse a different actual exposure.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles Series | 00:05:00 | Five-minute candle series; only finished candles update the channel and drive the two daily timing windows. |
| Session High Length | 108 | Number of finished candles used by formed-only Highest. With the default time frame and windows, 108 candles cover 01:00–10:00 UTC. |
| Session High Source | unset | Left unset. Highest automatically reads the High of each finished candle. |
| Session Low Length | 108 | Number of finished candles used by formed-only Lowest. Keep it equal to Session High Length so both boundaries describe the same session. |
| Session Low Source | unset | Left unset. Lowest automatically reads the Low of each finished candle. |
| Order Volume | 1 | Quantity assigned to each resting limit and to the reduce-only reset action. In the normal one-fill path it matches the resulting position; ReduceOnly prevents the reset action from increasing or reversing exposure if the actual size differs. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits only finished five-minute candles. Two formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks calculate rolling Highest 108 and Lowest 108 values directly from that stream.
- Two [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) blocks inspect candle OpenTime. The placement interval 09:55:00–09:59:59 acts when that bar completes at 10:00 UTC, while 00:55:00–00:59:59 acts on completion at 01:00 UTC. This one-bar offset is part of the configured timing, not an execution delay.
- A placement [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) emits once and stays set until reset. [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) blocks capture Highest, Lowest, Position, zero, and volume on that pulse; [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) and [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) admit one valid, flat-position order pair.
- The buy [Order registration](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) block places the first limit at Session Low. Its registered Order refreshes the high and volume inputs before triggering the sell registration at Session High. Both orders have ShrinkPrice false and remain working until filled or canceled.
- The [Level1](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html) block subscribes to BestBid continuously. The retained feed activates live quote processing for matching the resting limits, while their prices continue to come solely from the captured Highest and Lowest values.
- Each registered Order is stored. A buy MyTrade releases the stored sell Order to [Order cancellation](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html), and a sell MyTrade does the symmetric action. Reset invokes [Mass order cancellation](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html), then also releases both stored Order references to their cancellation blocks so cleanup is deterministic without relying on the bulk acknowledgement.
- The reset pulse finally triggers [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) with ReduceOnly, Order Volume, and the MarketOrder algorithm. In the intended sequential one-fill path this quantity closes the whole position; if fills race or actual exposure differs, ReduceOnly prevents the action from increasing or reversing it. The [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) receives the finished candles, Highest, Lowest, both Order streams, both limit MyTrade streams, and the reset-close MyTrade stream.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
