# Exp Sar Tm Plus Strategy

High-level StockSharp port of the **Exp_Sar_Tm_Plus** expert advisor. The strategy monitors Parabolic SAR reversals on a configurable timeframe and mirrors the original money-management and time-out features while keeping the logic compatible with the StockSharp high-level API.

## Trading logic

- Candles are subscribed from the `CandleType` parameter (default: 4-hour time frame). The Parabolic SAR indicator is calculated with the user-defined `SarStep` and `SarMaximum` coefficients.
- For every finished candle the algorithm buffers close prices and SAR values. The `SignalBar` parameter selects which closed candle is evaluated (default: the last closed bar) and compares it to the preceding candle to detect a change in SAR direction.
- A **long** position is opened when price crosses **above** the SAR (previous candle below SAR, selected candle above SAR) and long trading is enabled. Existing short exposure is closed automatically before switching direction.
- A **short** position is opened when price crosses **below** the SAR (previous candle above SAR, selected candle below SAR) and short trading is enabled. Existing long exposure is flattened first.
- Positions are closed when the SAR moves against them (`AllowLongExit` / `AllowShortExit`), when optional stop-loss / take-profit levels are breached, or when the maximum holding time (`UseTimeExit` + `HoldingMinutes`) expires.
- Stop-loss and take-profit levels are recalculated on every entry using the instrument `PriceStep`. Both levels are optional and ignored when the corresponding value is zero.

## Parameters

- `MoneyManagement` – fraction of the base `Volume` that will be traded on every entry. Values ≤ 0 fall back to the plain `Volume` value. Normalized to the instrument `VolumeStep`.
- `ManagementMode` – enumeration preserved from the original expert. All modes currently behave like `Lot` (fixed volume) inside this port.
- `StopLossPoints` / `TakeProfitPoints` – distance in price steps used to set protective levels around the entry price. Set to zero to disable.
- `DeviationPoints` – original slippage setting. It is kept for completeness, but the high-level API executes market orders without using this value.
- `AllowLongEntry`, `AllowShortEntry` – toggles for opening long/short positions.
- `AllowLongExit`, `AllowShortExit` – toggles for closing positions when price crosses the SAR in the opposite direction.
- `UseTimeExit` – enables position liquidation after `HoldingMinutes` minutes in the market.
- `HoldingMinutes` – duration for the time-based exit window.
- `CandleType` – candle data type for SAR analysis.
- `SarStep`, `SarMaximum` – Parabolic SAR configuration.
- `SignalBar` – number of closed candles to shift the signal evaluation (0 = current finished candle, 1 = previous, etc.).

## Risk management and notes

- The strategy invokes `StartProtection()` on start, enabling StockSharp built-in protection services.
- Time-based exits rely on the candle `CloseTime` (fallback to `OpenTime` if it is not available) to measure the holding period accurately.
- Only one net position is maintained at any time. Position reversals automatically close the opposite side before entering a new trade.
- The implementation keeps the parameter set of the original MQL5 expert. Some options (like non-`Lot` money management modes or order `DeviationPoints`) are placeholders because the high-level API abstracts broker-side mechanics.
