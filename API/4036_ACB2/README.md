# ACB2 Strategy

## Overview
The strategy reproduces the MetaTrader expert advisor **ACB2**, which is a diagnostic tool rather than a trading robot. It monitors the tick activity inside a selected candle frame (M1 by default) and prints detailed summaries about the ticks counted locally versus the tick volume reported by the data provider. The StockSharp port keeps the original alert-driven behaviour by streaming messages through `AddInfoLog`, making it suitable for plugging into any logging sink or notification pipeline.

## Parameters
- **Candle Type** – timeframe that defines the monitored frame. The default value is M1. Custom timeframes are supported as long as they are based on `TimeFrameCandleMessage`.

## Monitoring Logic
1. At start-up the strategy subscribes to:
   - Timeframe candles to obtain the open time and tick volume of the current and the previous frame.
   - Tick data to react to every new trade update.
2. The first candle snapshot is used to align the internal counters with the already accumulated tick volume. A `"Start"` log entry is emitted exactly once, mirroring the original `Alert("Start")` call.
3. For every subsequent tick the following actions take place:
   - Determine whether the tick belongs to the active frame or starts a new one by aligning the server time to the selected timeframe.
   - Increment the locally counted ticks for the active frame. When a new frame begins, move the previous counter to the `Frame 1` slot, reset the current counter to one and emit the "Frame 0 closed" notification that contains the previous frame open time, the locally counted ticks and the tick volume reported by the candle subscription.
   - Refresh the cached provider volumes using the latest candle snapshot so the diagnostic output can highlight mismatches between locally counted ticks and the external feed.
4. After the per-tick processing an aggregated status line is printed. It follows the original format and contains:
   - symbol identifier and timeframe label,
   - the open time of the current frame, elapsed seconds since that open, local tick counter and the provider tick volume,
   - the open time, local counter and provider volume for the previous frame.

## Logging Output
The strategy produces three types of log entries:
- `Start` – confirms the initial synchronisation with the candle feed.
- `Frame 0 closed: ...` – raised on the first tick of a new frame, summarising the previous one.
- `<symbol> <timeframe>: Frame 0 ...` – emitted on every tick and showing the live counters for the active and previous frames.

All lines are written via `AddInfoLog`, so they are immediately visible in StockSharp UI logs and in any external subscribers.

## Implementation Notes
- Only diagnostic data is collected; no orders or portfolio actions are generated.
- Tick volume is extracted from `ICandleMessage.TotalTicks` with fallbacks to `TotalVolume` and `Volume` in case the provider sends only aggregated volume.
- The strategy keeps two rolling frames in memory and never builds custom collections, complying with the conversion guidelines.
- When the data feed has not yet supplied a volume snapshot, the local counter is used as a temporary fallback so the logs stay consistent.
- The helper `DescribeDataType` function converts the selected timeframe into a compact label (M1, H1, etc.) for easier reading of the diagnostics.
