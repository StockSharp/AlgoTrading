# Fly System Scalp Strategy

## Overview
The Fly System Scalp Strategy is a high-frequency breakout system that reproduces the core behaviour of the original MQL4 expert advisor *FlySystemEA*. The strategy constantly monitors the best bid/ask quotes and deploys two symmetrical stop orders around the market price. The goal is to capture rapid micro-trends that emerge after short-term consolidations, while maintaining a strict control over spread, commissions and trading session boundaries.

The conversion focuses on the following key mechanics:

* Automatic placement of buy stop and sell stop orders at a configurable distance from the market.
* Automatic cancellation of pending orders when the spread (including commission) exceeds the admissible threshold or trading is outside the permitted session.
* Optional take-profit and mandatory stop-loss management attached directly to new pending orders.
* Support for manual fixed volume and automatic risk-based position sizing using broker contract specifications (price step, step value, lot step, min/max volume).
* Self-resetting trading cycle that waits for positions to be closed before arming a new pair of stop orders.

The StockSharp implementation leverages the high-level API (level-1 subscription with bind) and follows the required project conventions: strategy parameters are exposed through `StrategyParam`, comments are in English, and the namespace uses the file-scoped declaration.

## Trading Logic
1. **Level 1 Feed** – The strategy subscribes to level-1 data for the assigned security. Every update records the latest bid/ask pair.
2. **Validation Layer** – Before any trading action is taken, the engine checks:
   * The strategy is online and allowed to trade.
   * The current time is inside the optional trading window.
   * The spread plus commission does not exceed `MaxSpread` pips.
3. **Pending Order Placement** – When the above conditions hold, no position is open, and the strategy is ready for a new cycle, two orders are prepared:
   * Buy Stop at `Ask + PendingDistance * pip` with protective Stop Loss and optional Take Profit offsets.
   * Sell Stop at `Bid - PendingDistance * pip` with mirrored protections.
   Orders are re-registered when the difference between the desired and actual price reaches `ModifyThreshold` pips.
4. **Order Management** – If a position opens, the opposite pending order is cancelled immediately. When a trading cycle is interrupted by spread/time violations, all pending orders are removed and the strategy waits for valid conditions.
5. **Position Sizing** – When `AutoLotSize` is enabled, the volume is derived from `RiskFactor` percent of account equity divided by the loss per contract at the configured stop distance. Volume is rounded to the broker lot step and clamped to min/max limits.
6. **Protection** – `StartProtection()` is invoked so that StockSharp monitors the position for emergency liquidation if required by the infrastructure.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `PendingDistance` | Distance in pips between market price and both stop orders. | 4 |
| `StopLossDistance` | Stop-loss distance in pips attached to new positions. | 0.4 |
| `TakeProfitDistance` | Take-profit distance in pips when enabled. | 10 |
| `UseTakeProfit` | Enables take-profit placement. | `false` |
| `MaxSpread` | Maximum allowed spread (pips); 0 disables the filter. | 1 |
| `CommissionInPips` | Commission (in pips) added to the spread filter. | 0 |
| `AutoLotSize` | Enables risk-based position sizing. | `false` |
| `RiskFactor` | Percentage of equity used to size positions when auto sizing is active. | 10 |
| `ManualVolume` | Fixed volume used when auto sizing is disabled. | 0.1 |
| `UseTimeFilter` | Enables the trading session filter. | `false` |
| `TradeStartTime` | Session start time (inclusive). | 00:00:00 |
| `TradeStopTime` | Session stop time (exclusive). | 00:00:00 |
| `ModifyThreshold` | Price delta (pips) required before a pending order is re-registered. | 1 |

## Usage Notes
* Ensure that the target instrument provides `Step`, `PriceStep`, `StepPrice`, `LotStep`, `MinVolume`, and `MaxVolume` because automatic sizing relies on these values. When the data is missing the strategy gracefully falls back to `ManualVolume`.
* The pip value is estimated from the security decimal precision and price step, matching the logic from the original MQL implementation (including special handling for 3/5-digit Forex quotes).
* If `TradeStartTime` equals `TradeStopTime` while `UseTimeFilter` is enabled, the session is considered always open. When the start time is greater than the stop time, the session wraps around midnight.
* Spread validation adds `CommissionInPips` to the current spread, replicating the behaviour where the MQL version combined spread and commission into a single filter.
* The strategy does not create or manage chart objects. Visualisation can be added externally by binding the level-1 data to charts.

## Differences Versus the Original EA
* The low-level tick timer and GUI elements from the MQL version are intentionally omitted. The StockSharp variant relies on level-1 events and built-in logging.
* Order modification logic is simplified: when the target price differs by more than `ModifyThreshold` pips the order is re-registered, instead of the multi-branch adjustment logic present in the EA.
* Automatic commission detection from trade history is replaced by a static `CommissionInPips` parameter; however, the risk filter still adds this value to the spread before trading.
* The StockSharp version utilises `StartProtection()` instead of custom stop-level monitoring loops.

## Backtesting
The strategy requires level-1 quote data to reproduce the stop-order triggering logic. For historical simulations, supply bid/ask series or build synthetic level-1 data from tick history. Candle-only feeds are insufficient because pending stop orders must react to spread changes.
