# MaRobot Strategy

## Summary
- Implements a bar-based moving average crossover system that operates on a configurable intraday timeframe while using daily ADX and RSI filters.
- Uses StockSharp high-level bindings to calculate two simple moving averages together with `Lowest`/`Highest` swing detectors and daily `AverageDirectionalIndex` plus `RelativeStrengthIndex` indicators.
- Recreates the original MT4 protective logic: take-profit by percentage, swing-based stop loss, and an optional break-even stop once a minimum profit is achieved.

## Indicators
- `SimpleMovingAverage` (fast and slow) on the primary timeframe.
- `Lowest` / `Highest` to capture the extreme prices of the last `BackClose` candles for stop placement.
- Daily `AverageDirectionalIndex` and `RelativeStrengthIndex` values for trend-strength and momentum filters.

## Parameters
- `CandleType` – primary timeframe (default: 15-minute candles).
- `FastPeriod`, `SlowPeriod` – lengths of the fast and slow SMA lines.
- `AdxThreshold` – maximum allowed value of the daily ADX to enable new entries.
- `RsiThreshold` – daily RSI level for long entries (the short entry uses `100 - RsiThreshold`).
- `TakeProfitRatio` – fractional distance between entry price and the profit target.
- `StopLossPoints` – distance of the protective stop (in instrument points) that arms after reaching `ProtectThreshold`.
- `ProtectThreshold` – minimal open profit ratio that activates the protective stop.
- `BackClose` – number of completed candles used for swing high/low stop calculation.
- `DailyAdxPeriod`, `DailyRsiPeriod` – lengths of the daily indicators.

## Trading Rules
1. Work only on finished candles to match the MT4 expert advisor.
2. Wait until all indicators are fully formed and daily values are available.
3. **Entry filters**:
   - Reject new positions when the daily ADX exceeds `AdxThreshold`.
   - Long entry requires the fast SMA crossing above the slow SMA and the daily RSI to be below `RsiThreshold`.
   - Short entry requires the fast SMA crossing below the slow SMA and the daily RSI to be above `100 - RsiThreshold`.
4. On entry, store the swing extreme (`Lowest` for longs, `Highest` for shorts) as the manual stop reference.
5. **Exit logic** while a position is active:
   - Close at `TakeProfitRatio` profit measured from the stored entry price.
   - Close if the candle close breaches the stored swing stop level.
   - Close on an opposite moving-average cross.
   - After the profit exceeds `ProtectThreshold`, arm a break-even style stop offset by `StopLossPoints` (rounded to the tick size) and close if the price retraces through it.
6. Reset all internal state when the net position returns to zero.

## Notes
- All comments in the C# code are kept in English as required by the repository guidelines.
- The strategy relies solely on StockSharp high-level subscriptions via `Bind`, avoiding manual indicator buffers.
- Python translation is intentionally omitted according to the task instructions.
