# Return Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the classic "Return Strategy" expert advisor. It prepares a grid of paired buy-limit and sell-limit orders at the start of a configured trading window. The grid is symmetric around the market price, uses fixed spacing in pips, and can be sized either by a fixed volume or a percentage risk model. Once orders are filled the strategy supervises the position with static and trailing stop-loss logic, monitors cumulative open profit, and forces a full flattening at the daily cut-off time or every Friday.

The original system was designed for netting accounts and focused on capturing mean-reversion moves after scheduled times. The conversion keeps that structure while adapting order management, trailing, and capital controls to the StockSharp high-level API.

## Trading Rules

- **Daily preparation** – At the `StartHour` the strategy checks that no grid orders are active and places `PendingOrderCount` buy limits below and sell limits above the current price. The first level is offset by `DistancePips` and each subsequent level adds `StepPips` of spacing.
- **Risk control** – Each pending order can use either a fixed `OrderVolume` or a risk-based size derived from `RiskPercent`. When risk sizing is used the available capital and stop-loss distance determine the per-order volume so that the total grid risk equals the configured percentage.
- **Stop management** – Every filled position receives an initial stop loss based on `StopLossPips`. If `TrailingStopPips` is greater than zero, once price advances beyond the trailing threshold the stop is ratcheted in steps of `TrailingStepPips`.
- **Profit target and session exit** – The net open profit is tracked in pips. When it reaches `TotalProfitPips` the strategy marks all positions and orders for closure. It also performs the same flush at the configured `EndHour` and on every Friday regardless of profit.
- **Order expiration** – Pending orders can automatically expire after `ExpirationHours`. Expired or manually cancelled orders are removed from the tracking list to allow a new grid to be placed the next day.

## Parameters

| Parameter | Description |
| --- | --- |
| `StopLossPips` | Initial stop distance for any filled position (in adjusted pips). |
| `StartHour` | Hour (0–23) when the pending-order grid is created. |
| `EndHour` | Hour (0–23) that triggers a complete exit of positions and orders. |
| `TotalProfitPips` | Net open profit target (in pips) that forces all trades to be closed. |
| `TrailingStopPips` | Distance of the trailing stop from price once activated. Set to zero to disable trailing. |
| `TrailingStepPips` | Additional advance required before moving the trailing stop. Must be positive when trailing is enabled. |
| `DistancePips` | Initial offset for the first pending order on each side of the market. |
| `StepPips` | Incremental spacing between consecutive pending orders. |
| `PendingOrderCount` | Number of buy limits and sell limits to register at `StartHour`. |
| `ExpirationHours` | Lifetime of pending orders in hours. Zero disables expiration. |
| `OrderVolume` | Fixed volume per pending order. Leave at zero to enable risk-based sizing. |
| `RiskPercent` | Portfolio percentage allocated to the entire grid. Per-order size is derived from this value when `OrderVolume` is zero. |
| `CandleType` | Candle series used to drive timing and stop management logic. |

## Additional Notes

- The pip conversion mirrors the original MetaTrader logic by adjusting the step size for three- and five-decimal instruments.
- When `RiskPercent` is used, the percentage applies to the combined grid and is divided equally across all pending orders.
- The strategy enforces validation rules identical to the source EA: hours must be inside the daily range, trailing requires a non-zero step, and only one of `OrderVolume`/`RiskPercent` may be active at a time.
- All public comments in the code are provided in English for consistency with repository guidelines.
