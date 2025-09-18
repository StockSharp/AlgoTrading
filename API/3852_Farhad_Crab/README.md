# Farhad Crab Strategy

## Overview

The **Farhad Crab Strategy** is a StockSharp high-level port of the MetaTrader expert advisor `FarhadCrab1.mq4`. The original EA is a fast scalping system designed for the M1 timeframe on GBP/JPY, GBP/USD, and EUR/USD. This conversion recreates the trading logic in C# by combining intraday moving-average filters with a daily trend safety net and automated exit management.

The strategy analyses the current timeframe through a 9-period EMA calculated on the typical price and a 9-period SMA calculated on the candle open. At the same time it keeps track of a 55-period smoothed moving average (SMMA) built from daily candles. Whenever the short-term filters show sufficient momentum to the upside while no position is open, a long trade is triggered. Conversely, when the intraday high remains under the SMA of opens, a short trade is opened. The daily SMMA acts as a protective overlay: crossing the price from below forces all long trades to exit, and crossing from above closes short positions.

Exit management reproduces the original EA's behaviour with configurable take-profit levels in pips and independent trailing stops for long and short positions. The trailing logic follows the MetaTrader implementation by moving the stop only after the market advances by the configured distance. The strategy closes positions via market orders rather than pending stop orders, making it compatible with the high-level API event flow.

## Key Features

- **Indicator set identical to the EA** – 9-period EMA on typical price, 9-period SMA on opens, and a daily 55-period SMMA for trend direction.
- **Multi-timeframe data handling** – subscribes to the trading timeframe and to daily candles simultaneously, letting StockSharp compute the required indicators without manual buffering.
- **Configurable exits** – symmetric take-profit distances (long/short) and trailing stops expressed in pips, just like the original external inputs.
- **Daily safety switch** – replicates the EA's rule that closes longs when the daily SMMA moves above the daily close and shorts when it moves below.
- **Built-in protection** – calls `StartProtection()` once at startup to guard positions according to framework best practices.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Trade volume applied to new market orders. | `0.1` |
| `LongTakeProfitPips` | Take-profit distance for long positions, measured in pips. | `10` |
| `ShortTakeProfitPips` | Take-profit distance for short positions, measured in pips. | `10` |
| `LongTrailingStopPips` | Trailing stop distance for long trades. Trailing is disabled when set to zero. | `8` |
| `ShortTrailingStopPips` | Trailing stop distance for short trades. Trailing is disabled when set to zero. | `8` |
| `DailyMaPeriod` | Length of the daily smoothed moving average used for protective exits. | `55` |
| `CandleType` | Primary timeframe that drives the strategy calculations. Defaults to 1 minute candles. | `1m` |

All parameters are exposed through `StrategyParam<T>` and marked as optimisable where it makes sense, so they can be tuned via the StockSharp optimiser.

## Trading Rules

1. **Long entries**: When the current candle low stays above the 9-period EMA of the typical price and no position is active, open a long trade.
2. **Short entries**: When the current candle high remains below the 9-period SMA of the open price and no position is active, open a short trade.
3. **Daily protective exit (long)**: Close any long position if the daily SMMA moves above the daily close while it was previously below the previous close.
4. **Daily protective exit (short)**: Close any short position if the daily SMMA moves below the daily close while it was previously above the previous close.
5. **Take-profit**: Close the position once the configured pip target is reached.
6. **Trailing stop**: After a position gains the trailing distance, lock in profits by monitoring the retracement distance and exit when the price moves back by that amount.

## Implementation Notes

- The code relies exclusively on high-level `SubscribeCandles().Bind(...)` calls, eliminating any manual indicator buffers and staying within the project's guidelines.
- Pips are calculated from the instrument's `PriceStep` with the usual MetaTrader-style adjustment for 3- and 5-digit quotes. This keeps the behaviour consistent with the EA's point-based parameters.
- Stop-loss and take-profit management is performed internally by closing positions when conditions are met, rather than registering limit/stop orders. This approach matches the instantaneous exits found in the original script while remaining compatible with asynchronous order execution in StockSharp.
- The strategy resets its state inside `OnReseted`, ensuring that optimisation runs and repeated launches start from a clean slate.

## Usage Tips

- The original EA was tailored for highly volatile GBP and EUR pairs on the M1 timeframe. Similar results can be expected when applying the same timeframe and instruments, but the parameters are exposed to accommodate different volatility profiles.
- Because the system keeps only one position at a time, it is suitable for straightforward backtesting and live execution without complex position pyramiding.
- Trailing stops become more effective on instruments with smooth trends. On ranging markets consider reducing the trailing distance or relying solely on take-profit exits.
- The daily SMMA exit serves as the primary risk control. For swing-oriented setups you can increase `DailyMaPeriod` to make the long-term filter less reactive.
