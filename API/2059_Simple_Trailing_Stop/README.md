# Simple Trailing Stop Strategy

This example demonstrates how to manage an open position with a trailing stop using StockSharp's high level API.

## Overview
- Opens a single long position after receiving the first finished candle.
- Enables position protection with a trailing stop.
- The stop price follows the current price at a fixed distance.

## Parameters
- `TrailPoints` – distance in price points used to trail the stop.
- `CandleType` – type of candles processed by the strategy.

## Logic
1. On start the strategy subscribes to candles and enables `StartProtection` with trailing.
2. After the first completed candle the strategy buys at market price.
3. When price moves in favour of the position the stop level is moved to keep the distance defined by `TrailPoints`.
4. If price reverses and touches the trailing stop, the position is closed automatically.

The strategy is simplified and intended to show basic trailing stop usage.
