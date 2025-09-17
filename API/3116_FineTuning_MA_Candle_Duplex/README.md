# FineTuning MA Candle Duplex Strategy

## Overview
- C# port of the MetaTrader 5 expert advisor **Exp_FineTuningMACandle_Duplex**.
- Replicates the FineTuningMA candle indicator in two independent streams so that long and short logic can be tuned separately.
- Designed for StockSharp's high-level strategy API: subscriptions, indicators, risk management and chart drawing are all managed automatically by the framework.

## FineTuningMA candle model
- The original indicator builds a synthetic candle by applying three weighted exponents (`Rank1`–`Rank3`) and corresponding shift coefficients to the last `Length` bars.
- The resulting weighted open and close values are compared to generate a color code: `2` for bullish, `1` for neutral, `0` for bearish.
- When the real candle body is smaller than the configurable `Gap`, the synthetic open is flattened to the previous synthetic close. This reproduces the "flat body" logic from the MQL5 version.
- The indicator in this port emits only the color stream (decimal values 0/1/2) because the trading rules depend exclusively on the color transitions.

## Trading logic
1. Subscribes to two candle feeds (`LongCandleType` and `ShortCandleType`). They can point to the same timeframe or different ones.
2. For each feed a dedicated FineTuningMA indicator instance is created with its own weighting parameters and signal offset (`SignalBar`).
3. Finished candle events are processed with the following rules:
   - **Long exit** – if the previous color equals `0` the existing long position is closed.
   - **Long entry** – if the previous color equals `2` and the current color changed away from `2`, a buy order is sent (after covering any short position).
   - **Short exit** – if the previous color equals `2` the existing short position is covered.
   - **Short entry** – if the previous color equals `0` and the current color changed away from `0`, a sell order is sent (after covering any long position).
4. Order volume is controlled by `OrderVolume`. When a reversal is required the strategy automatically adds the absolute current position so the position flips in a single market order.
5. Optional protective barriers (`TakeProfitPoints`, `StopLossPoints`) are translated into price points and applied through `StartProtection`.

## Parameters
### Long stream
- `LongCandleType` – candle data type (timeframe) for the long indicator stream.
- `LongLength` – number of bars used in the weighted calculation.
- `LongRank1`, `LongRank2`, `LongRank3` – exponent coefficients that shape the weight curve across the lookback window.
- `LongShift1`, `LongShift2`, `LongShift3` – additional modifiers (0…1) that bias the weights toward the beginning or the end of the window.
- `LongGap` – maximal size of the real candle body that keeps the synthetic open price equal to the previous synthetic close.
- `LongSignalBar` – how many completed candles to skip before reading the signal (`0` evaluates the last closed candle, `1` uses the previous one, etc.).
- `EnableLongEntries` – toggles long entries.
- `EnableLongExits` – toggles automatic long exits.

### Short stream
- `ShortCandleType` – candle data type for the short indicator stream.
- `ShortLength`, `ShortRank1`, `ShortRank2`, `ShortRank3`, `ShortShift1`, `ShortShift2`, `ShortShift3`, `ShortGap`, `ShortSignalBar` – identical to their long-side counterparts but applied to the short stream.
- `EnableShortEntries` – toggles short entries.
- `EnableShortExits` – toggles automatic short exits.

### Trading
- `OrderVolume` – base quantity for new positions. Reversals automatically add the absolute current position to this value.
- `TakeProfitPoints` – optional take-profit distance expressed in price points (0 disables it).
- `StopLossPoints` – optional stop-loss distance expressed in price points (0 disables it).

## Notes
- The original expert advisor included money-management modes based on balance or margin. The port exposes a simpler fixed `OrderVolume` parameter. Adjust it to match the desired position sizing.
- `StartProtection` is invoked only when the instrument exposes a valid price step (`Security.Step > 0`).
- No Python version is provided intentionally (as requested).
- Chart areas are created automatically: if long and short candle feeds differ, two separate panels are displayed; otherwise only one chart is shown.
- The strategy relies on finished candles; it does not react to intrabar updates.
