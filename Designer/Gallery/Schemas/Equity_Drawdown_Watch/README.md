# Equity Drawdown Watch
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram maintains an equity curve from strategy P&L, keeps a persistent peak, logs each new drawdown breach once, and runs a deliberately sparse protected long cycle so the monitored account values move during a backtest.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute BTCUSDT candles provide the single sampling clock. A parallel Best Bid Level 1 subscription keeps live unrealized-P&L valuation active between candle samples.
- P&L change updates silent realized and unrealized latches at its native event rate. Each finished candle releases both latest values with Start Balance into `Equity = Start Balance + Realized P&L + Unrealized P&L` exactly once.
- `max(stored peak, equity)` maintains the all-run equity peak. Formed-only Highest(2) confirms that monotonic peak stream and introduces one sample of warm-up without shortening the peak history.
- Drawdown is `(Peak - Equity) / Peak * 100`. Comparison verifies `Drawdown >= Drawdown Alert`, while Crossing and a Boolean latch forward only a new upward threshold crossing to the log.
- Modify position opens one market long from flat. Absolute protection closes it, and a 1,440-candle timer permits the next entry only after five days of subsequent five-minute candles.

## Entry and Exit Rules

- **Long entry**: After Highest(2) is formed, an available entry Flag releases Volume 1 to a Buy, OpenPosition, MarketOrder Modify position block. The first entry is therefore attempted on the second finished candle.
- **Short entry**: The diagram does not open short positions. Sell trades are protective exits from the long position.
- **Exit**: Position protection submits a market exit after a favorable move of 0.04 or an adverse move of 0.03 in absolute price units. The entry fill starts the 1,440-subsequent-candle cooldown; the timer, not the exit fill, resets entry readiness.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| BTC Data Security | BTCUSDT@BNBFT | Instrument used by the Candles, Level 1, and Strategy trades subscriptions. Set Strategy Security to the same instrument for transactions and P&L. |
| Candle Series | 00:05:00 | Finished candle interval and the clock for equity samples and cooldown steps. |
| Start Balance | 1000 | Baseline added to realized and unrealized P&L when equity is calculated. |
| Peak Confirmation Length | 2 | Highest length over the monotonic persistent-peak stream; it delays trading until the second sample. |
| Drawdown Alert, % | 1 | A log is written when drawdown reaches or exceeds this level from below. |
| Volume | 1 | Quantity of every long entry. |
| Entry Cooldown N | 1440 | Number of subsequent finished five-minute candles between permitted entries, equal to five days. |
| Take Distance | 0.04 | Favorable absolute price distance that closes the long at market. |
| Stop Distance | 0.03 | Adverse absolute price distance that closes the long at market. |

## Diagram Details

- The BTC [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) feeds finished [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html), Best Bid [Level 1](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html), and [Strategy trades](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html). The trading blocks use Strategy Security and Strategy Portfolio.
- Silent Variable latches separate the event-driven [P&L change](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) stream from the candle clock. Their fixed release order gives each [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) one complete, same-candle input set.
- The persistent peak starts at zero, so changing Start Balance remains valid. The max Formula updates that state before its monotonic output enters formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) Highest(2).
- [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) and [Crossing](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) receive the threshold and drawdown in a fixed order. A false recovery crossing is ignored; a true rising crossing releases the saved percentage through [String Formatter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) to a Log [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html).
- The cooldown [Delay](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) block consumes each candle before any same-candle decision. A Buy fill arms N = 1440, and its output resets the entry Flag before the eligible candle reaches Highest.
- The Buy [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) fill initializes local [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). The chart shows candles, sampled equity, peak, drawdown, both P&L components, entry fills, protective fills, and all strategy fills.

## Usage

Import the `.json` file into Designer, set Strategy Security to BTCUSDT@BNBFT, and run it on the packaged March history. Verify the equity scale, absolute protection distances, alert percentage, and five-day cooldown for your instrument before using the diagram with live trading.
