# AIS2 Trading Robot

## Overview
The AIS2 Trading Robot is a multi-timeframe breakout system converted from the original MetaTrader 5 expert advisor. It scans a higher timeframe (default 15-minute candles) to detect directional breakouts, while a faster timeframe (default 1-minute candles) provides adaptive trailing stops. Order placement, risk budgeting, and trailing logic follow the rules encoded in the legacy MQ5 version, but are implemented on top of StockSharp's high-level strategy API.

## Trading Logic
- **Primary signal candle**: For every finished candle on the primary timeframe the strategy captures the high, low, close, midpoint, and range.
- **Long setup**:
  - Previous close must be above the candle midpoint, signalling bullish pressure.
  - The current ask price must trade above the previous high plus the measured spread (breakout confirmation).
  - Entry price is the current ask. Stop loss equals `high + spread - (range × StopFactor)`. Take profit equals `ask + (range × TakeFactor)`.
  - Additional broker safety checks ensure both risk and reward are greater than the configured stop buffer distance.
- **Short setup**:
  - Previous close must be below the midpoint, signalling bearish pressure.
  - The current bid must print below the previous low (downside breakout).
  - Entry price is the current bid. Stop loss equals `low + (range × StopFactor)`. Take profit equals `bid - (range × TakeFactor)`.
- **Conflict resolution**: New trades are taken only when the strategy is flat or positioned in the opposite direction (the entry volume automatically offsets the existing exposure before opening the new position).

## Order Management
- **Trailing stop**: The secondary timeframe range is multiplied by `TrailFactor` to build a dynamic trail. For long positions the stop is pulled to `bid - trailDistance`; for shorts it is pushed to `ask + trailDistance`. Trailing updates are skipped when the price is not in profit or when the requested modification is smaller than the configured trail step and freeze buffers.
- **Profit taking & stop exit**: Both long and short positions are liquidated with market orders whenever bid/ask prices cross the stored stop loss or take profit levels.
- **Order book feed**: A live order book subscription keeps track of the current best bid/ask prices so that the strategy can reproduce the MQ5 logic that relied on `SymbolInfo.Ask/Bid` values.

## Position Sizing & Risk Controls
- **Account reserve**: A configurable fraction of portfolio equity is locked and cannot be used for trading. This replicates the `Inp_aed_AccountReserve` parameter from the original EA.
- **Order reserve**: The remaining capital is further limited by an order allocation fraction that caps the maximum risk budget per trade.
- **Risk checks**:
  - If the reserved equity is smaller than the allocation limit (`Equity × OrderReserve`), the strategy refuses to place new trades.
  - Position size is computed as `riskBudget / |entry - stop|`, aligned to the security volume step. When no portfolio information is available the fallback `BaseVolume` parameter is used.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `AccountReserve` | Fraction of equity withheld from trading (0–0.95).
| `OrderReserve` | Fraction of tradable equity that defines the risk budget per trade (0–1).
| `PrimaryCandleType` | Working timeframe for breakout detection (default 15 minutes).
| `SecondaryCandleType` | Faster timeframe that drives trailing stop updates (default 1 minute).
| `TakeFactor` | Multiplier applied to the primary range to compute the take-profit distance.
| `StopFactor` | Multiplier applied to the primary range to compute the stop-loss distance.
| `TrailFactor` | Multiplier applied to the secondary range to compute the trailing distance.
| `BaseVolume` | Fallback order size used when portfolio metrics are not available.
| `StopBufferTicks` | Additional distance (in ticks) required beyond exchange stop constraints.
| `FreezeBufferTicks` | Extra buffer that prevents minor trailing adjustments near the freeze level.
| `TrailStepMultiplier` | Spread multiplier that defines the minimum increment between trailing updates.

## Notes
- Always feed the strategy with both primary and secondary candle series plus a live order book stream to unlock all logic branches.
- The breakout checks rely on bid/ask prices, so paper trading with last-trade prices only may deliver different behaviour compared to a real environment.
- Position protection is started automatically once the strategy runs, mirroring the safety routines present in the MQ5 version.
