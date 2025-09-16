# Next Bar Momentum Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts that occur when the most recent completed bar closes far away from an older reference bar. It was inspired by the "Nextbar" MetaTrader expert advisor and keeps the original money-management features such as pip-based stops, trailing logic, and limited position lifetime.

The default configuration aims at fast-moving FX or index futures charts on the 15-minute timeframe, but the logic works on any symbol that provides regular candles. Every order is sent at market using the configured position size.

## Trading logic

- **Signal detection**
  - When a new bar finishes, the algorithm compares the close of the previous bar with the close that occurred `SignalBar` bars ago.
  - If the previous close is higher than the distant close by more than `MinDistancePips`, a long setup is generated.
  - If the previous close is lower than the distant close by more than `MinDistancePips`, a short setup appears.
  - The `ReverseSignals` switch flips the direction of every setup to match contrarian workflows.
- **Order handling**
  - Orders are ignored while a position is open. The strategy only holds a single position at a time just like the original expert advisor.
  - Every fill stores the entry price and pre-computes the stop-loss and take-profit levels in price units. Pip-based values are converted using the security price step (5-digit instruments automatically use a 10× multiplier to match MetaTrader pip size).

## Exit rules

- **Stop loss / take profit** – Both levels are optional. A value of zero disables the corresponding protection. The strategy monitors candle highs and lows to trigger exits when levels are crossed.
- **Trailing stop** – When enabled (`TrailingStopPips` > 0), the stop is moved closer to the current price once profit exceeds `TrailingStopPips + TrailingStepPips`. The distance from price to stop never shrinks, ensuring a monotonic trailing behaviour.
- **Position lifetime** – After staying in the market for `LifetimeBars` completed candles, the position is closed on the next bar open regardless of profit. This reproduces the original "expire after N bars" mechanism.

## Parameters

- `CandleType` – Timeframe used for signal evaluation. Defaults to 15-minute time-frame candles.
- `OrderVolume` – Quantity sent with each market order.
- `StopLossPips` – Distance from the entry price to the protective stop, expressed in pips.
- `TakeProfitPips` – Distance from the entry price to the profit target, expressed in pips.
- `TrailingStopPips` – Distance maintained by the trailing stop. Set to zero to disable trailing logic.
- `TrailingStepPips` – Additional profit required before the trailing stop is advanced again. Ignored when trailing is disabled.
- `SignalBar` – Number of bars between the comparison closes. Must be at least two to avoid referencing the current bar.
- `MinDistancePips` – Minimum pip distance between the compared closes before a signal is accepted.
- `LifetimeBars` – Maximum number of completed candles that a position may remain open. Set to zero to disable the timer.
- `ReverseSignals` – Inverts long/short signals when enabled.

## Implementation notes

- The strategy relies on a short rolling list of previous closes rather than heavy historical structures, which keeps the signal calculation lightweight.
- Pips are converted into price units using the security price step. Instruments quoted with 3 or 5 decimal places automatically map to the traditional pip definition.
- All risk controls are enforced on completed candles. If you need intra-bar protection, combine the strategy with exchange-native stop orders through the platform configuration.
- No automated tests are supplied with this sample. Validate it on historical data before using it in production.
