# Twenty Pips Price Channel Strategy

## Overview

The Twenty Pips Price Channel Strategy is a conversion of the original MetaTrader expert advisor *20 pips* that combines a Donchian-style price channel with short-term moving-average filters. The algorithm opens trades only when the current candle opens opposite to the previous one, filters direction with moving averages calculated on typical prices, and manages exits through a fixed twenty-pip target supported by a dynamic channel-based trailing stop.

The StockSharp version keeps the spirit of the original approach while adapting order management to the high-level API. Market orders are used for entries and exits, profit targets are monitored internally, and stop levels are emulated with price-channel conditions.

## Trading Logic

1. **Indicator stack**
   - A one-period simple moving average of the typical price (H+L+C)/3 acts as a fast baseline that mirrors the previous candle's typical price.
   - A configurable slow simple moving average (default 20) calculated on closing prices plays the role of the `MA_Low` filter from the EA.
   - Highest and lowest indicators with the same period as the price channel (default 20) emulate the original custom indicator buffers.

2. **Entry conditions**
   - Long setup: the previous fast typical price is above the previous slow moving average **and** the current candle opens below the previous open. After a losing trade the volume is multiplied by the recovery factor (default 2). The entry price is recorded to track profit and loss.
   - Short setup: the previous fast typical price is below the previous slow moving average **and** the current candle opens above the previous open. Volume scaling follows the same recovery logic as for long trades.

3. **Exit management**
   - A fixed take-profit target equal to `TakeProfitPips` multiplied by the instrument price step is placed when the position opens.
   - A channel-driven trailing stop mimics the original `OrderModify` call. When the previous bar breaks beyond the price channel (two-bar shift from the MT4 logic), the protective stop is moved to the previous extreme minus/plus the trailing offset in pips. If the next candle gaps beyond that extreme, the position exits immediately at the open price.
   - Take-profit, trailing stop, and gap exits are all executed through market orders while tracking the actual exit price to update the win/loss flag for the martingale-style scaling.

4. **Martingale recovery**
   - After every closed losing position, the next entry size is multiplied by `RecoveryMultiplier`. Profitable trades reset the flag and revert to the base volume.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Primary timeframe used for calculations. | 1 hour candles |
| `ChannelPeriod` | Lookback period for the Donchian-style channel. | 20 |
| `SlowMaPeriod` | Length of the slow moving average filter. | 20 |
| `TakeProfitPips` | Distance in pips for the fixed profit target. | 20 |
| `TrailingOffsetPips` | Offset used when tightening the stop to the previous extreme. | 10 |
| `RecoveryMultiplier` | Volume multiplier applied after a loss. | 2 |
| `Volume` | Base trading volume before recovery scaling. | 0.1 |

## Usage Notes

- The strategy expects `Security.PriceStep` to reflect the pip value of the traded instrument. Adjust `TakeProfitPips` and `TrailingOffsetPips` if the symbol uses a different pip definition.
- Because StockSharp uses market orders for exits, backtests may show slippage compared to the original MT4 stop and limit orders. The logic still reproduces the same price thresholds.
- The channel values are shifted to emulate the `iCustom(..., shift=2)` calls; keep this in mind when modifying the trailing behaviour.
- The recovery multiplier can be set to 1 to disable martingale-style scaling.
