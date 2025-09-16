# Stop Loss Take Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This port replicates the MetaTrader "Stop Loss Take Profit" expert advisor. The strategy flips a coin whenever the account is flat and opens a market order in the chosen direction. Each position immediately receives pip-based stop-loss and take-profit orders. If the stop is hit the next trade doubles its size (capped by the security's volume limits). A take-profit resets the volume back to the initial amount. The behaviour mirrors the original martingale-style position sizing while using StockSharp's high-level API.

## Trading Logic

- **Market Data**: Uses the `CandleType` parameter (default 1-minute time frame) to drive decision points.
- **Entry Rules**:
  - When `Position == 0` and no entry order is pending, the strategy generates a pseudo-random boolean.
  - `true` opens a long position with `BuyMarket(volume)`; `false` opens a short with `SellMarket(volume)`.
- **Exit Rules**:
  - Protective stop-loss and take-profit orders are placed as soon as the entry fill is received.
  - A stop exit doubles the size for the next trade, while a take-profit resets it.
  - If either stop or take-profit distance is set to `0`, the respective protective order is skipped.
- **Money Management**:
  - `InitialVolume` defines the base order size.
  - After a losing trade the size is doubled but clipped to `Security.MaxVolume` when that value is available.
  - Volume is normalised to the instrument's `VolumeStep`, `MinVolume` and `MaxVolume` so orders remain valid.
- **Pip Handling**:
  - By default the strategy infers a pip from the instrument's `PriceStep` and `Decimals` (5-digit FX symbols map to 0.0001).
  - Set `PipSize` to a positive value to override the automatic pip size detection.

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `CandleType` | 1-minute candles | Time frame used to trigger coin flips and entries. |
| `StopLossPips` | 1 | Stop-loss distance expressed in pips. `0` disables the stop. |
| `TakeProfitPips` | 1 | Take-profit distance expressed in pips. `0` disables the take-profit. |
| `InitialVolume` | 0.01 | Starting trade volume. Doubled after stop-loss events and reset after wins. |
| `PipSize` | 0 (auto) | Optional pip size override in absolute price units. |

## Usage Notes

- Works on both long and short sides and is intentionally direction-neutral.
- Protective orders are cancelled whenever the position is closed to avoid stale orders.
- The random generator is seeded with `Environment.TickCount`, meaning each session produces different trade sequences.
- Suitable for demonstrating risk layering and martingale behaviour rather than for production trading without further risk controls.
