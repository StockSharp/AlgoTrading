# Money Fixed Risk Strategy

## Overview
Money Fixed Risk Strategy is a direct port of the MetaTrader 5 expert advisor **Money Fixed Risk.mq5**. The original script periodically computes the maximum position size that keeps risk below a fixed percentage of account equity and then opens a market buy protected with symmetric stop-loss and take-profit orders. This StockSharp version preserves the same behaviour using the high-level tick subscription API and risk controls provided by the framework.

The strategy listens to every trade (tick) for the selected security. After a configurable number of ticks it evaluates the current portfolio value, converts the configured stop distance in pips to price units and calculates the largest volume that keeps risk within the specified percentage of equity. If the calculated volume is valid the strategy opens a long market order and assigns both stop-loss and take-profit levels at exactly the stop distance away from the filled price. The stop and target are monitored on each subsequent tick and the position is closed once either boundary is touched.

## Data requirements
- Tick (trade) data is required because the entry condition counts individual ticks. Candle data is not used.
- Correct `PriceStep`, `StepPrice`, `VolumeStep`, `MinVolume` and optional `MaxVolume` must be configured for the security so that the position sizing formula matches the broker contract specifications.

## How the strategy works
1. Wait for tick updates through `SubscribeTrades()`.
2. Track the last traded price and increment an internal counter.
3. Whenever the tick counter reaches **Ticks Interval**, reset the counter and:
   - Determine the pip size from `PriceStep` and `Decimals` (5- and 3-digit quotes are automatically scaled by 10).
   - Convert the configured stop-loss distance from pips to price units.
   - Determine the current account equity (tries `Portfolio.CurrentValue`, falls back to `CurrentBalance`, then `BeginValue`).
   - Compute the monetary risk per contract using the stop distance and `StepPrice`.
   - Derive the maximum volume that keeps the monetary risk below `Risk %` of equity and normalise it to the exchange volume step and limits.
4. If the calculated volume is positive, send a buy market order sized to flatten any existing short exposure and open a new long position.
5. Record the stop-loss and take-profit prices around the entry price. On every subsequent tick monitor the trade price and close the position if either level is violated.

## Parameters
- **Stop Loss (pips)** – stop-loss distance expressed in pips. The take-profit is placed at the same distance in the opposite direction.
- **Risk %** – percentage of portfolio equity risked on each trade.
- **Ticks Interval** – number of ticks to wait before re-evaluating and potentially opening a new position.

All parameters support optimisation and validation (must be greater than zero).

## Money management details
- Risk amount = `Equity * (Risk % / 100)`.
- Stop distance in price units = `Stop Loss (pips) * pip size`, where pip size equals `PriceStep * 10` for 3- and 5-decimal instruments, otherwise `PriceStep`.
- Monetary risk per contract = `(stop distance / PriceStep) * StepPrice`.
- Position size = `Risk amount / monetary risk per contract`, rounded down to the nearest `VolumeStep` and constrained by `MinVolume`/`MaxVolume`. Orders are skipped when the normalised size is below the minimum volume.

## Differences from the original expert advisor
- Runs entirely inside StockSharp without calling MetaTrader libraries.
- Uses `StartProtection()` so that platform-level protections remain active.
- Relies on the strategy portfolio for current equity information instead of querying terminal balance objects.
- Uses continuous tick monitoring to exit positions, removing the need for explicit stop orders in this educational example.

## Usage notes
- This sample opens only long positions just like the original file. Extend `ProcessTrade` if short trades are required.
- When backtesting ensure that tick data includes enough depth to reach the configured tick interval; otherwise no trades will be triggered.
- Because position sizing depends on broker metadata, verify the correctness of `PriceStep`, `StepPrice` and volume constraints before running live.
- The implementation avoids using indicator collections to respect the conversion guidelines and keeps the logic stateful through private fields.
