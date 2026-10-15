# Fee Accounting per Fill Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades confirmed hourly CCI(30) threshold returns with one fixed-volume market order and demonstrates commission accounting for each observed fill. A four-candle cooldown controls new signals, an educational percentage-protection layer can close the position, and one log receives both the calculated per-fill charge and the strategy engine's cumulative commission value.

![schema](schema.svg)

## Strategy Overview

- Finished one-hour candles feed CommodityChannelIndex 30. The indicator emits every value, including values produced before its length is fully formed.
- A buy requires previous CCI < -100 and current CCI >= -100. A sell requires previous CCI > 100 and current CCI <= 100.
- The buy side also requires Position <= 0, the sell side requires Position >= 0, and both require the four-candle cooldown to be ready.
- Each accepted signal submits exactly one market order of fixed Volume. From a flat position it opens the signalled side; against a unit position it closes that position to zero and does not open the other side in the same signal.
- Position protection is an explicit educational layer with a 1% take-profit and a 0.7% fixed stop-loss. It checks only finished candle closes and skips the close of any candle that already produced a signal order.
- Each observed fill produces one synthetic charge using Trade.Price × Trade.Volume × Commission Rate % / 100. The chart shows CCI, orders, fills and both commission series, while formatted commission messages are written to one log stream.

## Entry and Exit Rules

- **Long entry**: When the previous CCI is below -100 and the current CCI returns to -100 or higher, Position <= 0 and the cooldown is ready, submit one market buy of Volume. From flat this opens a long; against a unit short it only closes the short to zero.
- **Short entry**: When the previous CCI is above 100 and the current CCI returns to 100 or lower, Position >= 0 and the cooldown is ready, submit one market sell of Volume. From flat this opens a short; against a unit long it only closes the long to zero.
- **Exit**: An opposite eligible CCI signal can flatten a unit position with one fixed-volume market order. Independently, the educational protection layer can close the tracked exposure at a 1% take-profit or a 0.7% fixed stop-loss; its price input receives only finished closes from candles with no signal order.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 01:00:00 | One-hour time frame; only finished candles drive CCI decisions, cooldown increments and protection price checks. |
| CCI Length | 30 | Length of CommodityChannelIndex; the block emits values without waiting for the indicator to become fully formed. |
| Lower Level | -100 | Lower CCI threshold. A return upward through -100 creates the buy-side crossing condition. |
| Upper Level | 100 | Upper CCI threshold. A return downward through 100 creates the sell-side crossing condition. |
| Cooldown | 4 | Number of finished candles required after a fill before another signal may trade. |
| Commission Rate % | 0.04 | Percentage rate used only by the displayed per-fill formula `Trade.Price × Trade.Volume × rate / 100`. |
| Take Profit % | 1 | Favourable percentage move used by the educational position-protection layer. |
| Stop Loss % | 0.7 | Adverse percentage move used by the fixed, non-trailing stop in the educational protection layer. |
| Volume | 1 | Fixed quantity of every buy or sell signal order. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits finished hourly candles. Its close is stored for protection, while an [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) block calculates CCI 30 with formed-only filtering disabled. Per-candle flags preserve all four previous/current threshold comparisons until the final decision pulse.
- [Current position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) supplies the Position <= 0 and Position >= 0 gates. The cooldown starts at 4, is incremented and capped before each candle decision, and resets to 0 on every direct signal-order fill and protection fill. Consequently, finished bars 1, 2 and 3 after a fill are blocked and bar 4 is eligible.
- Two [Order registration](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) blocks submit the fixed-Volume market buy and sell. Each block's direct trade output updates protection and resets the cooldown; a dedicated Trades for order block observes the registered Order and supplies the signal-fill stream used by the synthetic fee calculation and chart.
- Both direct signal-fill streams enter [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html), allowing its internal position to return to zero after a signal close. Its own fill output is not fed back into that input. The no-signal gate releases the stored finished close for a protection check only when neither signal order fired on that candle.
- For each observed buy, sell or protection fill, converters store Trade.Price and Trade.Volume in silent latches. A release pulse then emits rate, price and volume in that order; the [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) updates `a × b × r / 100`, and the silent fee state is released last exactly once for that observed fill.
- The [Strategy P&L](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) Commission output is the engine's cumulative commission and remains zero when the test or live environment has no commission rule configured. The synthetic per-fill formula is a displayed calculation and does not write into that engine value.
- Two [String formatter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) outputs are merged by Combination<IComparable> and sent to one Log [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html). Trades for order subscribes after it receives the registered Order, so an execution environment that completes an order inside the registration call can produce a fill before that observer is attached; the direct trade output still drives protection and cooldown in that case.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
