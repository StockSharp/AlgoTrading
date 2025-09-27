# Trailing Profit Guardian Strategy

## Overview
The Trailing Profit strategy ports the MetaTrader 4 expert advisor `trailing_profit.mq4` into the StockSharp framework. The original script does not open trades – it supervises the floating profit of all open orders and, once a desired gain is reached, arms a trailing profit lock. If the unrealized profit subsequently retraces by a configurable percentage, the strategy immediately liquidates the entire position to protect accumulated gains. The StockSharp version keeps the same behaviour while leveraging the high-level API for market data subscriptions, logging and built-in protections.

## How it works
1. The strategy subscribes to tick trades through `SubscribeTrades()` to continuously monitor the last transaction price of the configured security.
2. When a trade arrives, it calculates the current floating profit as `Position * (lastPrice - PositionPrice)`. If no position is open, the internal trailing state is reset.
3. Once the floating profit becomes strictly greater than `ActivationProfit`, the trailing logic is armed. The first trailing floor equals `profit - profit * TrailPercent / 100` and represents the minimum profit that must be preserved.
4. Every time a new profit high appears the floor is raised by applying the same percentage to the fresh profit peak. This mirrors the MetaTrader expert which stores `profit_off = profit - profit*(percent/100)` whenever a higher profit is recorded.
5. If the floating profit falls back below the current floor, the strategy sends market orders to close the entire position. It resubmits the closing order whenever the residual volume changes (for example after partial fills) to make sure the position is flattened.
6. When the position size returns to zero the trailing state (armed flag, floor value and helper variables) is reset, allowing a new trailing cycle to begin as soon as fresh positions are opened manually or by another strategy.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `TrailPercent` | `33` | Percentage of the peak floating profit that is surrendered before liquidation. A value of `33` keeps 67% of the gain. |
| `ActivationProfit` | `1000` | Minimum floating profit that must be exceeded before the trailing logic is armed. |

## Implementation notes
- `GetWorkingSecurities` returns a single `(Security, DataType.Ticks)` pair so the trailing manager reacts on each trade update and does not depend on candle completions.
- `ProcessTrade` contains the ported logic from `trailing_profit.mq4`. It activates the trailing guard, raises the floor, and triggers liquidation exactly like the MQL loop, while also writing informative log messages about each state transition.
- `ExecuteLiquidation` keeps track of the last close order volume and direction, preventing duplicate market orders while still resending whenever the remaining position size changes.
- `OnPositionChanged` resets the trailing state whenever the strategy becomes flat. This mirrors the MQL behaviour where `close_start` clears the state once all orders have been closed.
- `StartProtection()` is invoked during `OnStarted` so StockSharp's built-in protection module (stop-loss, take-profit, etc.) can be configured if desired, even though the core logic already guards profit.

## Usage
Attach the strategy to a security whose open position you want to supervise. The module will not generate new entries; it simply guards the floating profit of the current position. You can use it alongside another entry strategy or manual trading to automatically secure gains once they reach the configured thresholds.
