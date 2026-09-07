# Pending Orders by Time Virtual Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram uses finished five-minute candles to arm two symmetric virtual breakout levels once per day. The candle stamped 02:00 supplies the reference close; a later touch of either level registers one market entry, percentage protection manages the fill, and the candle stamped 22:00 clears the setup and flattens any remaining position. The virtual levels are stored values rather than resting exchange orders.

![schema](schema.svg)

## Strategy Overview

- Only finished five-minute candles drive timing, level calculations, breakout checks, and protective price updates. The opening window `02:00:00–02:04:59` selects exactly the candle stamped 02:00, which is processed when it completes around 02:05.
- If the position is flat during that opening pulse, the diagram stores the candle close and calculates `Upper Level = Close × 1.0015` and `Lower Level = Close × 0.9985`. Both retained values remain fixed and are replaced at the next qualified opening pulse.
- An armed-state variable and an ordered end-of-candle trigger chain ensure that High, Low, and both retained levels are refreshed before a decision. A shared one-shot Flag admits only the first breakout in the daily setup.
- The upper comparison is evaluated before the lower comparison. If one candle spans both levels, only the upper breakout is accepted and the diagram registers one market buy; otherwise the lower breakout can register one market sell. No order exists before a level is touched.
- Entry fills initialize Position protection. Finished candle closes then drive its 2% take-profit and 0.5% stop-loss checks. The closing window `22:00:00–22:04:59` disarms any unused setup and submits a market ReduceOnly action limited to Order Volume 1; its fill is returned to the protection block so tracked exposure is cleared.

## Entry and Exit Rules

- **Long entry**: While the virtual pair is armed, `High ≥ Upper Level` wins the daily one-shot gate and registers a market buy for Order Volume 1. The same event disables both breakout paths until the next valid opening pulse.
- **Short entry**: If the upper breakout was not accepted, an armed `Low ≤ Lower Level` condition wins the one-shot gate and registers a market sell for Order Volume 1. It also disables both breakout paths for the rest of the cycle.
- **Exit**: Position protection submits a market exit when a finished candle close reaches 2% in favor of the actual entry fill or 0.5% against it. Independently, the candle stamped 22:00 disarms the virtual pair and requests a market ReduceOnly close of at most Order Volume 1. An intrabar protection threshold that is not present at the finished candle close is not acted on, and the setup does not re-arm after an exit until the next opening window.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles Series | 00:05:00 | Finished five-minute candles used for timing, virtual levels, breakout tests, and protective close-price checks. |
| Opening Window | 02:00:00–02:04:59 | Inclusive one-candle interval that captures the reference close when the position is flat. |
| Closing Window | 22:00:00–22:04:59 | Inclusive one-candle interval that disarms an unused setup and flattens an open position. |
| Entry Distance | 0.15% | Symmetric percentage offset above and below the captured close. |
| Take Profit | 2% | Favorable close-price movement from the actual entry fill that activates protection. |
| Stop Loss | 0.5% | Adverse close-price movement from the actual entry fill that activates protection. |
| Order Volume | 1 | Quantity submitted by either market entry and the maximum scheduled position reduction. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits finished five-minute candles. Close, High, and Low [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) blocks provide explicit numeric streams.
- Two [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) blocks inspect candle OpenTime. Their upper bounds stop one second before the next five-minute stamp because both configured boundaries are inclusive.
- [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) blocks retain the reference close, calculated levels, armed state, chosen side, and constants. Two [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) blocks calculate the symmetric percentage offsets only during a qualified opening pulse.
- [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html), [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html), and [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) blocks enforce flat-only arming, refreshed-value evaluation, buy-first precedence, and one accepted breakout per setup.
- The buy and sell [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) blocks are market-order actions. Their MyTrade outputs initialize the shared [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) block, whose Price input receives finished candle closes.
- The scheduled [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) block uses ReduceOnly with Order Volume 1. It derives the closing direction from current exposure, never increases the position, and returns its MyTrade output to Position protection after a timed exit.
- The [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) shows the finished candles, both retained virtual levels, entry and protective orders, and entry, protective, and scheduled-close trades.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
