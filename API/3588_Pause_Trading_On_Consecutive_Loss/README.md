# Pause Trading On Consecutive Loss Strategy

The **Pause Trading On Consecutive Loss Strategy** reproduces the risk control logic of the MetaTrader 4 expert advisor *"Pause Trading On Consecutive Loss"*. The original script monitored the most recent closed trades, counted how many of them ended with a negative profit, and suspended new orders when the losing streak exceeded a user-defined limit within a short time window. The StockSharp port keeps that behaviour while wrapping it around a minimal momentum entry model so the pause mechanism can be evaluated inside the standalone strategy.

## How it works

1. The strategy subscribes to time-frame candles specified by `CandleType`. Whenever a finished candle arrives, the closing price is compared to the previous close. If it increased, the strategy attempts a long entry; if it decreased, a short entry is considered. Positions exit whenever a bullish position faces a bearish candle (close below open) or a bearish position faces a bullish candle (close above open).
2. After every closed position the realised profit of the strategy is inspected. Losing results enqueue their closing timestamp in an internal FIFO list that only stores consecutive losses. Profitable or breakeven exits wipe the list, just as the MQL loop aborted once it encountered a non-losing deal.
3. When the list reaches `ConsecutiveLosses` items, the strategy checks whether the time difference between the oldest and the newest loss is within `WithinMinutes`. If it is, trading is paused until `PauseMinutes` elapse from the last closing time. During the pause no new market orders are submitted, but the existing position management continues operating so the book can flatten naturally.
4. Once the pause expires, the list of losses is cleared and trading resumes automatically. The behaviour mimics the original `CheckLastNLossDifference` and `lastOrderCloseTime` functions without relying on a persistent order history scan.

The implementation uses StockSharp's high-level candle subscriptions (`SubscribeCandles`) and the built-in PnL manager to monitor realised profits. A simple queue (`Queue<DateTimeOffset>`) captures the timestamps of the loss streak while respecting the prohibition on redundant manual history traversal.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 5-minute time frame | Candle aggregation used for the simple momentum entries. |
| `OrderVolume` | `0.1` | Volume (in lots/contracts) sent with each entry and exit order. |
| `ConsecutiveLosses` | `3` | Number of back-to-back losing positions required before new trades are paused. |
| `WithinMinutes` | `20` | Maximum number of minutes allowed between the first and the last loss in the streak. A value of zero disables the window check. |
| `PauseMinutes` | `20` | Duration of the trading suspension after the loss streak is detected. |

## Notes

- The queue of loss timestamps is only populated when the strategy is flat and has just realised a loss. Partial closes or profitable trades do not extend the streak, preventing false positives.
- The pause timer is evaluated against each finished candle. If `PauseMinutes` elapse while the strategy is idle, the next candle immediately unlocks trading.
- Because the StockSharp version operates on a netting position, the realised PnL difference is derived from `PnLManager.RealizedPnL`, faithfully mirroring the MetaTrader history lookup without reprocessing the entire order log.
