# PSAR Multi Timeframe Strategy

## Overview
The strategy replicates the MetaTrader expert advisor **EA_PSar_002B**. It evaluates Parabolic SAR values on three timeframes (M15, M30 and H1) while managing positions on a one-minute stream. Trading is directional: only one net position can be active at a time and new trades appear only when the previous exposure is flat. The original expert was designed for EURUSD on the M1 chart and the port keeps the same context.

## Trading logic
1. **Parabolic SAR convergence filter** – the latest SAR values from M15, M30 and H1 must lie within 19 minimum price steps of each other. This keeps the three curves “tight” before a breakout is allowed.
2. **Long entry** – one of the following sequences has to occur:
   - M15, M30 and H1 SAR values are below their respective current lows, the previous H1 SAR was above the previous H1 high, and the new H1 SAR drops below the current H1 low.
   - M15 and H1 SAR are below their current lows while the previous M30 SAR was above the previous M30 high and the new M30 SAR drops below the current M30 low.
   - M30 and H1 SAR are below their current lows while the previous M15 SAR was above the previous M15 high and the new M15 SAR drops below the current M15 low.
3. **Short entry** – mirror conditions of the long setup with highs/lows inverted.
4. **Take profit / stop loss** – limits are expressed in points (minimum price increments). By default the target equals 999 points and the protective stop equals 399 points, which correspond to the MQL values after normalising 4/5-digit quotes.
5. **Dynamic exit** – while a position is open the M30 SAR is monitored.
   - Longs close when the previous SAR was below the previous M1 low but the current SAR jumps above the current M1 high.
   - Shorts close when the previous SAR was above the previous M1 high but the current SAR drops below the current M1 low.
   - When the current M30 SAR crosses beyond the entry price the stop is trailed to that SAR level.

## Money management
`UseMoneyManagement` reproduces the money-management switch from the EA. When disabled the `FixedVolume` parameter is used. When enabled the requested percentage of portfolio capital is converted to a synthetic “lot” size using the same formula as the MQL version (percent of free capital divided by 100,000). The amount is aligned to `Security.VolumeStep` and clipped to the broker limits (`VolumeMin`/`VolumeMax`).

## Parameters
- `BaseCandleType` – timeframe used for trade management (defaults to M1).
- `FastSarCandleType`, `MediumSarCandleType`, `SlowSarCandleType` – timeframes for the SAR filters (defaults: 15m, 30m, 60m).
- `EnableParabolicFilter` – mirrors the `sar2` flag from MQL; switching it off stops trading completely.
- `TakeProfitPoints`, `StopLossPoints` – offsets in points (minimum price increments). Pip size is derived from `Security.PriceStep` and `Security.Decimals` to handle 3/5-digit forex quotes correctly.
- `UseMoneyManagement`, `PercentMoneyManagement`, `FixedVolume` – volume controls described above.

## Conversion notes
- Only the high-level StockSharp API is used. All price series are subscribed through `SubscribeCandles().Bind(...)` and indicator data is received through bindings instead of manual buffers.
- Protective orders are implemented by explicit market exits, exactly like the original script that called `OrderClose`.
- The broker digit coefficient from MQL is replaced by automatic pip size detection (`PriceStep` × 10 for 3/5-digit instruments).
- The EA forbade trading on non-EURUSD symbols or non-M1 charts by printing messages. In StockSharp the strategy logs remain silent, but the behaviour is documented here.

## Usage tips
1. Attach the strategy to EURUSD with one-minute candles for the base subscription. The indicator timeframes can still be changed if experimentation is desired.
2. Make sure the security metadata exposes `PriceStep`/`Decimals`. Without them the stop and target distances fall back to a unit size of 1.
3. Keep `EnableParabolicFilter` enabled; it is equivalent to the EA’s master switch. Disable it only when you intentionally want the strategy to stay idle.
