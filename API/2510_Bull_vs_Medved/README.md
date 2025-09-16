# Bull vs Medved Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Bull vs Medved is a limit-order breakout strategy originally published for MetaTrader 5. It watches the last three completed candles and only allows trades during six five-minute windows spread evenly through the day. When a specific bullish or bearish candle sequence appears, the strategy places a pending limit order offset from the current spread and protects the position with symmetrical stop-loss and take-profit targets.

## Trading Logic

1. **Trading windows** – orders are evaluated only if the current time of day is within one of the six configurable windows (00:05, 04:05, 08:05, 12:05, 16:05, 20:05 by default) and within the configured duration (5 minutes by default). Leaving the window resets the one-order-per-window guard.
2. **Required candle data** – the strategy waits for three finished candles before generating any signals. Calculations always use the three most recent completed candles.
3. **Bullish setups**:
   - **Regular bull**: the candle three periods ago closes above the open of the second candle, the second candle has at least a 1-pip bullish body, and the most recent candle has a bullish body larger than the configured `CandleSizePips` threshold.
   - **Bad bull filter**: if all three candles have large bullish bodies, the signal is ignored to avoid parabolic moves.
   - **Cool bull**: after a strong bearish pullback (second candle closes at least 2 pips below its open), the most recent candle must engulf the pullback and print at least 40% of the normal `CandleSizePips` body. Either a regular bull (without the bad-bull filter) or a cool bull pattern triggers a long setup.
   - On a valid long signal the strategy places a **buy limit** order below the best ask by `IndentUpPips` (converted to instrument price units).
4. **Bearish setup**:
   - If the most recent candle has a bearish body larger than `CandleSizePips`, the strategy places a **sell limit** order above the best bid by `IndentDownPips`.
5. **Risk management** – once a position is opened the strategy automatically attaches absolute stop-loss and take-profit targets using the configured pip distances.
6. **Order management** – only one order can be sent per window and no new order is placed while another limit order for the same symbol remains active.

## Parameters

- `OrderVolume` – trade volume for limit orders.
- `CandleSizePips` – minimum bullish/bearish body size for the latest candle.
- `StopLossPips` – protective stop distance from entry price.
- `TakeProfitPips` – profit target distance from entry price.
- `IndentUpPips` – buy limit offset below the best ask.
- `IndentDownPips` – sell limit offset above the best bid.
- `EntryWindowMinutes` – duration of each allowed trading window.
- `CandleType` – candle timeframe used to evaluate patterns.
- `StartTime0` … `StartTime5` – start times for the six trading windows.

## Additional Notes

- The strategy subscribes to the order book to maintain the latest bid/ask prices for precise limit placement. If no book data is available it falls back to the latest candle close.
- Price offsets are calculated in pip-sized units that adapt automatically to 3- and 5-digit quotes.
- Stop-loss and take-profit are applied through `StartProtection` so the targets follow the actual fill price of the limit order.
