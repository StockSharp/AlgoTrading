# Sample Check Pending Order Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Sample Check Pending Order strategy continuously ensures that exactly one buy-stop and one sell-stop order are resting in the book. The original MetaTrader 5 expert by Tungman verifies that the broker accepts the requested lot size, confirms there is sufficient free margin for both directions, and then submits new pending orders right on top of the current bid/ask with a one-day expiration. This conversion reproduces the same workflow using StockSharp's high-level order management API and Level 1 quotes.

## Trading Logic

1. **Market data processing**
   - The strategy subscribes to Level 1 updates and caches the latest best bid and best ask prices.
   - Trading logic is suspended until both sides of the book are known and `IsFormedAndOnlineAndAllowTrading()` confirms the environment is ready (strategy is running, the portfolio is connected, etc.).
2. **Volume validation**
   - Each incoming tick triggers a validation of the configured `OrderVolume` against `Security.MinVolume`, `Security.MaxVolume`, and `Security.VolumeStep`.
   - The check mirrors the MT5 helper: volume must lie within the allowed range and be an exact multiple of the step. Violations produce an informational log entry and block any new orders.
3. **Margin pre-check**
   - Before submitting anything, the strategy estimates the margin required to place a long or short position of the configured size. It uses the latest bid/ask, the instrument multiplier, and the user-provided `AccountLeverage` to compute the requirement.
   - If the current or initial portfolio value is insufficient for either direction, the algorithm aborts for that tick, closely mimicking the `CheckMoneyForTrade` safeguards.
4. **Pending order placement**
   - When no active buy-stop order exists, a new one is registered at the current ask (rounded to the nearest tick). The same rule applies to the sell-stop at the current bid. Both orders reuse the same volume validation result.
   - Expiration is enforced manually: each order stores its time limit (`ExpirationMinutes`, one day by default). Future ticks cancel the order if the deadline has passed and immediately clear the slot for a new pending order.
5. **Risk management**
   - `StartProtection` wires an absolute stop-loss and take-profit based on `StopLossPoints` and `TakeProfitPoints`. Once an order is triggered, StockSharp automatically submits the protective exits at the configured distances, recreating the SL/TP parameters used in the MT5 version.

The end result is a minimalist breakout engine that always keeps the market "boxed" between two stop orders. Whenever one order is filled, the other side remains active while the strategy prepares to reissue the missing leg at the next quote update.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Lot size sent with each stop order. Must respect the broker limits and volume step. |
| `StopLossPoints` | Distance in points converted to price units for the protective stop once a trade is opened. |
| `TakeProfitPoints` | Distance in points used for the profit target created after a fill. |
| `ExpirationMinutes` | Lifetime of each pending order. When the period expires the order is cancelled and recreated on the next tick. |
| `AccountLeverage` | Estimated account leverage used to approximate margin requirements before each submission. |

All distances are transformed into actual price offsets using `Security.PriceStep`. If the instrument does not expose a valid price step or multiplier, the strategy falls back to a value of `1` to keep calculations defined. Logging messages document any abnormal configuration so operators can adjust parameters quickly.

## Implementation Notes

- **Order lifecycle** – The strategy tracks the latest `Order` objects returned by `BuyStop` and `SellStop`. Helper methods discard references once the order transitions to `Done`, `Canceled`, or `Failed`, ensuring that stale orders are not mistaken for active ones.
- **Expiration handling** – StockSharp exchanges do not universally support server-side expiry for stop orders. Instead of relying on broker-specific fields, the strategy monitors the timestamps locally and calls `CancelOrder` when a pending order outlives its deadline.
- **Margin approximation** – Margin availability is estimated using portfolio equity and the configured leverage. This keeps the behaviour close to `OrderCalcMargin` without requiring exchange-specific implementations.
- **High-level API usage** – All operations rely on the high-level `SubscribeLevel1`, `BuyStop`, `SellStop`, and `StartProtection` helpers, which matches the conversion guidelines and keeps the code concise.

This documentation intentionally contains extensive detail so that traders can understand every nuance of the conversion and confidently adapt the parameters to their broker environment.
