# Previous Candle Breakdown Strategy

The **Previous Candle Breakdown Strategy** watches the high and low of the most recently closed candle from a user-defined timeframe (default: 4 hours). Whenever the live candle pierces beyond those reference levels by a configurable indent, the strategy opens breakout trades. An optional moving average trend filter keeps trades aligned with the prevailing direction, while layered exit logic (fixed stop loss, take profit, and pip-based trailing stop) manages risk after entry.

## Key Features

- Uses a higher timeframe candle as the breakout anchor. All signals originate from the high or low of the last completed reference candle.
- Supports four moving average types (SMA, EMA, Smoothed, WMA) with independent shifts for the fast and slow lines. When both periods are non-zero the filter requires the fast MA to be above/below the slow MA before accepting long/short trades.
- Converts pip-based distances (indent, stop loss, take profit, trailing stop and step) into price units using the security settings. For 3 or 5 decimal instruments the pip equals 10 price steps, mirroring the original MQL logic.
- Allows position sizing either via fixed volume or by risking a percentage of account equity relative to the stop loss distance.
- Limits the maximum number of entries per direction and optionally closes all open positions once floating profit reaches a specified cash amount.
- Trailing stop logic emulates the MQL5 expert: after price advances beyond the trailing offset plus step, the stop level ratchets forward in discrete steps.

## Parameters

| Name | Description |
|------|-------------|
| `CandleType` | Timeframe used to build the previous candle reference (default: 4 hours). |
| `IndentPips` | Pip distance added above the high or below the low before triggering entries. |
| `FastPeriod` / `SlowPeriod` | Moving average lengths. Set either to 0 to disable the trend filter. |
| `FastShift` / `SlowShift` | Horizontal shift (in bars) applied to each moving average before comparison. |
| `MaType` | Moving average calculation method (Simple, Exponential, Smoothed, Weighted). |
| `StopLossPips` | Pip distance for the initial protective stop. Set to 0 to disable. |
| `TakeProfitPips` | Pip distance for take profit orders. Set to 0 to disable. |
| `TrailingStopPips` | Trailing stop distance. Requires `TrailingStepPips` &gt; 0. |
| `TrailingStepPips` | Minimum pip improvement before the trailing stop updates. |
| `OrderVolume` | Fixed trade volume. Leave at 0 to size positions by risk percentage. |
| `RiskPercent` | Percentage of portfolio equity to risk per trade when `OrderVolume` is 0. Requires a non-zero stop loss. |
| `MaxPositions` | Maximum number of entries allowed per direction. |
| `ProfitClose` | Closes all open positions when floating profit reaches this value (base currency). |

## Trading Logic

1. Track the most recent completed candle of `CandleType` and store its high/low.
2. On every update of the current candle:
   - Apply the moving average filter if enabled. Without sufficient MA history the strategy waits.
   - Compute breakout levels: previous high + indent and previous low − indent.
   - When the current candle high crosses the upper level, open a long position (subject to filters, max position count, and per-candle entry lockout).
   - When the current candle low crosses the lower level, open a short position using the same checks.
3. After entry the strategy attaches stop loss and take profit levels (if configured) and keeps them in memory. When price touches either boundary the position is closed via market order.
4. Trailing stop activation mirrors the MQL expert advisor: price must exceed the trailing offset plus the trailing step before the stop is moved. Subsequent updates require another full `TrailingStepPips` improvement.
5. Floating profit is recalculated each tick from the average entry price. If it reaches `ProfitClose`, all open exposure is liquidated immediately.
6. For risk-based sizing the strategy converts the pip stop distance into currency using the security's `PriceStep` and `StepPrice`. The resulting volume respects `MaxPositions` for scaling.

## Notes

- Set `TrailingStopPips` to 0 to disable trailing. If you enable trailing, ensure `TrailingStepPips` is also positive; otherwise no trailing updates will occur.
- The strategy stores entry timestamps per candle to avoid multiple entries on the same reference bar, matching the original EA behavior.
- For instruments without `PriceStep`/`StepPrice` metadata, risk-based sizing cannot be computed and trades will be skipped unless `OrderVolume` is specified.
- All comments in the code are written in English to align with project guidelines.

## Files

- `CS/PreviousCandleBreakdownStrategy.cs` – C# implementation of the strategy.

Python translation is not provided for this strategy per request.
