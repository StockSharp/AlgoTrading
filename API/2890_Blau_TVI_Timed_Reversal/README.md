# Blau TVI Timed Reversal Strategy

## Summary
- Converted from the MetaTrader 5 expert advisor **Exp_BlauTVI_Tm.mq5** located in `MQL/21014`.
- Re-implemented Blau Tick Volume Index (TVI) logic with three configurable smoothing stages.
- Generates reversal trades when the smoothed TVI changes slope and optionally restricts orders to a user-defined trading session.
- Supports optional stop-loss and take-profit protections defined in price points.

## Blau Tick Volume Index logic
The original expert uses the custom `BlauTVI` indicator that counts upticks and downticks from tick volume and smooths the result several times. The C# port keeps the same idea:

1. **Raw uptick/downtick counts**
   - `UpTicks = (Volume + (Close - Open) / PriceStep) / 2`
   - `DownTicks = Volume - UpTicks`
   - Candle volume is used as a proxy for tick volume because the StockSharp feed does not expose tick counts for aggregated candles.
2. **Stage 1 smoothing** – `UpTicks` and `DownTicks` are smoothed with the selected moving-average type (`EMA`, `SMA`, `SMMA`, `WMA`, `JMA`) and length `Length1`.
3. **Stage 2 smoothing** – the stage-1 outputs are smoothed again with length `Length2`.
4. **TVI calculation** – `TVI = 100 * (Up2 - Down2) / (Up2 + Down2)`.
5. **Stage 3 smoothing** – the TVI is smoothed one more time with length `Length3` to reduce noise.

The strategy stores a short rolling history of the final TVI values in order to replicate the `SignalBar` offset used by the original EA (`CopyBuffer` with shift `SignalBar`).

## Trading rules
- **Signal slope detection**
  - When the previous TVI value (`SignalBar + 1`) is less than the older value (`SignalBar + 2`), the TVI is considered to be turning upward. If the latest available value is also greater than the previous one, a bullish reversal signal is produced.
  - When the previous TVI value is greater than the older value, the TVI is turning downward. If the latest value is below the previous one, a bearish reversal signal is produced.
- **Position management**
  - Long entries require `EnableBuyOpen = true`, the bullish signal above, and a non-positive current position. The strategy closes any existing short before buying by adding the absolute short size to the configured `Volume`.
  - Short entries require `EnableSellOpen = true`, the bearish signal, and a non-negative position.
  - Long exits are triggered when the TVI slope turns bearish and `EnableBuyClose = true`.
  - Short exits are triggered when the TVI slope turns bullish and `EnableSellClose = true`.
- **Time filter**
  - When `EnableTimeFilter = true` the strategy only opens new positions inside the [`StartHour`:`StartMinute`, `EndHour`:`EndMinute`] window. Overnight sessions are supported (start > end). Positions are force-closed as soon as the time moves outside the session.
- **Protection**
  - `StopLossPoints` and `TakeProfitPoints` are converted to absolute price distances by multiplying with the instrument `PriceStep` and passed to `StartProtection`. Set to zero to disable.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `Volume` | Order size used for each entry (additional contracts are added to cover opposite exposure).
| `CandleType` | Candle data type/timeframe used for all calculations (default: 4-hour time frame).
| `MaType` | Moving-average type for all smoothing stages (EMA, SMA, SMMA, WMA, JMA).
| `Length1`, `Length2`, `Length3` | Smoothing lengths for each stage of the Blau TVI calculation.
| `SignalBar` | Offset for the TVI values used in signal generation (1 matches the previous closed candle like the MQL version).
| `EnableBuyOpen`, `EnableSellOpen` | Allow opening long/short positions on signals.
| `EnableBuyClose`, `EnableSellClose` | Allow closing existing long/short positions when slope reverses.
| `EnableTimeFilter` | Toggle for the trading-session window.
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Session bounds in exchange time. Supports intraday and overnight ranges.
| `StopLossPoints`, `TakeProfitPoints` | Fixed protective distances expressed in price points (0 disables each protection).

## Implementation notes
- The StockSharp environment does not expose tick counts for aggregated candles, therefore candle volume is used in place of tick volume. This keeps the behaviour close to the original indicator while remaining compatible with available data.
- The strategy tracks only a compact TVI history (a few most recent values) to reproduce the `SignalBar` shift without violating the repository guideline that discourages heavy custom collections.
- `StartProtection` is initialized only when a valid `PriceStep` is available; otherwise it falls back to protection without fixed targets.
- All comments were rewritten in English to comply with repository rules, and tabs are used for indentation as required by `AGENTS.md`.

## Usage tips
1. Start with the default H4 timeframe and EMA smoothing, which match the original expert advisor settings.
2. Adjust `SignalBar` to 0 if you prefer acting on the last closed candle instead of waiting one bar, but remember this deviates from the MQL logic.
3. When running on instruments with irregular trading hours, configure the time filter to avoid taking signals during illiquid periods.
4. Combine with portfolio-level money management if you need dynamic sizing; `Volume` is static by design, mirroring the fixed-lot approach of the source EA.
