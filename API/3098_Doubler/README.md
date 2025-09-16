# Doubler Hedged Trailing Strategy

## Overview
The **Doubler Hedged Trailing Strategy** is a direct StockSharp high-level API conversion of the MetaTrader 5 expert advisor `Doubler.mq5`. The algorithm instantly opens a symmetrical long and short market position whenever no exposure exists, then manages both legs with independent stop-loss, take-profit, and trailing-stop rules. The conversion preserves the hedging behaviour of the original MQL program while adapting risk management to StockSharp primitives (market orders, Level1 subscriptions, and strategy parameters).

Unlike directional strategies, the system keeps both directions active until each leg exits on its own protective logic. Once *both* legs are flat the strategy recreates the hedge, continuously maintaining paired exposure.

## Key Features
- **Automatic hedging** – opens one buy and one sell order with the same volume whenever the strategy has no active positions.
- **Pip-based risk controls** – stop-loss, take-profit, and trailing offsets are configured in pips and internally converted to price steps by inspecting the security price step and decimal precision (3/5 decimal instruments are automatically scaled by a factor of 10).
- **Independent trailing per leg** – each leg tracks the current best bid/ask. When the price moves more than `TrailingStopPips + TrailingStepPips` in favour, the stop level is advanced by `TrailingStopPips` while respecting the trailing step condition, exactly mirroring the original EA logic.
- **Volume validation** – order volume is validated against `MinVolume`, `MaxVolume`, and `VolumeStep`, raising an exception when the requested size violates exchange constraints.
- **Optional diagnostics** – the `LogTradeDetails` flag enables detailed informational messages (entries, exits, trailing adjustments) that help during testing or live monitoring.

## Parameters
| Parameter | Description | Default | Notes |
|-----------|-------------|---------|-------|
| `OrderVolume` | Volume of each hedge leg (buy and sell orders). | `1` | Must respect exchange volume limits; normalised to the closest `VolumeStep`. |
| `StopLossPips` | Stop-loss distance in pips. | `150` | `0` disables the stop-loss. |
| `TakeProfitPips` | Take-profit distance in pips. | `300` | `0` disables the take-profit. |
| `TrailingStopPips` | Trailing-stop distance in pips. | `5` | If greater than zero, `TrailingStepPips` must also be positive. |
| `TrailingStepPips` | Minimal additional move before the trailing stop advances. | `5` | Guard rail that prevents the stop from moving too frequently. |
| `LogTradeDetails` | Enables verbose logging of fills and trailing updates. | `false` | Set to `true` for debugging runs. |

## Trading Logic
### Entry
1. Subscribe to Level1 updates (best bid/ask).
2. When both `_longPosition` and `_shortPosition` are null and no entry orders are pending, register two market orders: one buy and one sell with `OrderVolume` each.
3. After fills are confirmed the strategy records entry prices, initial stop/take levels, and resets trailing trackers.

### Risk Management
- **Stop-loss** – for each leg the initial stop is placed `StopLossPips` away from the entry price. A stop distance of `0` disables the protective stop entirely.
- **Take-profit** – symmetric take-profit at `TakeProfitPips`. A value of `0` disables profit targets.
- **Forced closure** – if `NormalizeVolume` detects an invalid size (too small/large or not matching `VolumeStep`) the strategy throws an exception to prevent sending an invalid order.

### Trailing Stop Behaviour
1. When the price moves favourably by at least `TrailingStopPips + TrailingStepPips`, the stop is advanced to `currentPrice ± TrailingStopPips`.
2. The trailing step check reproduces the MQL condition: the stop only moves if the new level is at least `TrailingStepPips` closer to price than the existing stop, or if no stop exists yet.
3. For long positions the best bid is used as the reference price; for short positions the best ask is used so exit levels reflect realistic execution prices.

### Exit
- Each leg exits independently whenever its stop-loss, trailing stop, or take-profit condition is met. Exit orders are registered as market orders, and once a leg is flat its internal state is cleared.
- After both legs are closed the next Level1 update triggers a brand new hedged pair.

## Data Requirements
- **Level1 (best bid/ask)** – required for entry price snapshots, trailing calculations, and exit triggers.
- No candle or trade subscription is necessary; the strategy reacts exclusively to Level1 updates.

## Notes on the Conversion
- Pip distances are converted to absolute price offsets by multiplying with the security `PriceStep`. Instruments quoted with 3 or 5 decimals automatically receive a ×10 adjustment, matching the pip definition used in the MetaTrader expert.
- The strategy relies on StockSharp’s high-level `Strategy` methods (`RegisterOrder`, `StartProtection`, `SubscribeLevel1`) and avoids low-level connector operations.
- Hedging is implemented through internal `PositionState` objects so that long and short legs are tracked even when the broker/portfolio uses net positions.
- The conversion is self-contained and does not modify or require any test harness from the repository.
