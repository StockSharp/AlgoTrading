# Starter Strategy

The **Starter Strategy** is a conversion of the MetaTrader 5 expert advisor "Starter (barabashkakvn's edition)". The system waits for the Commodity Channel Index (CCI) to rebound from extreme oversold or overbought territory and confirms the move with the slope of a long-term moving average. When the momentum agrees with the trend filter, the strategy opens a single market position whose size is determined by a configurable risk percentage of the portfolio. Protective stops and an optional trailing mechanism reproduce the money-management rules from the original expert.

## Trading Logic

- **Trend filter** – a configurable moving average (MA) must be rising faster than `MaDelta` to allow long trades and falling faster than `MaDelta` to allow short trades. The strategy supports the same smoothing methods as the MQL version (simple, exponential, smoothed, linear weighted).
- **CCI confirmation** – the Commodity Channel Index must cross back above `-CciLevel` from below to trigger long entries and cross back below `CciLevel` from above to trigger shorts. The indicator is evaluated on finished candles only, mirroring the original bar-by-bar processing.
- **Single position model** – the algorithm keeps at most one open position. New signals are ignored until the current trade is closed, matching the MetaTrader logic that filters by magic number and symbol.

### Entry Rules

1. Wait for the close of a candle.
2. Calculate the latest and previous values of the moving average at the configured shifts.
3. Calculate the current and previous CCI readings.
4. **Go long** when:
   - The moving average slope exceeds `MaDelta` (current MA minus previous MA).
   - The current CCI value is greater than the previous one.
   - The CCI crosses upward through `-CciLevel` (previous below the threshold, current above).
5. **Go short** when:
   - The moving average slope is below `-MaDelta`.
   - The current CCI value is smaller than the previous one.
   - The CCI crosses downward through `CciLevel` (previous above the threshold, current below).

### Exit Rules

- **Initial stop-loss** – if `StopLossPips` is greater than zero, the filled entry price is offset by `StopLossPips * PriceStep` to compute an initial protective stop.
- **Trailing stop** – when both `TrailingStopPips` and `TrailingStepPips` are positive, the stop is advanced whenever price improves by at least the configured step. Long trades move the stop to `Close - TrailingStop`, shorts to `Close + TrailingStop`.
- **Manual exit** – if price touches the stop level inside the candle range, the strategy closes the position with a market order and resets the protection state.

## Risk Management

- **Position sizing** – base volume is `Portfolio.CurrentValue * MaximumRisk / price`. When the broker or back-end reports an invalid equity value, the strategy falls back to the manual `Volume` property (default 1).
- **Loss streak reduction** – after two or more consecutive losing trades the volume is reduced by `volume * losses / DecreaseFactor`, mimicking the original `DecreaseFactor` rule. Any profitable trade resets the loss counter.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `MaximumRisk` | `0.02` | Fraction of equity risked per trade when sizing the position. |
| `DecreaseFactor` | `3` | Lot reduction divisor applied after two or more consecutive losing trades. |
| `CciPeriod` | `14` | Number of bars used by the Commodity Channel Index. |
| `CciLevel` | `100` | Oversold/overbought threshold for CCI crossings. |
| `CciCurrentBar` | `0` | Shift of the current CCI value (0 = latest candle). |
| `CciPreviousBar` | `1` | Shift of the previous CCI value. |
| `MaPeriod` | `120` | Period of the moving average trend filter. |
| `MaMethod` | `Simple` | Moving average smoothing method (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaCurrentBar` | `0` | Shift applied to the moving average value. |
| `MaDelta` | `0.001` | Minimum slope difference between current and previous MA readings. |
| `StopLossPips` | `0` | Initial stop-loss distance in pips (0 disables the stop). |
| `TrailingStopPips` | `5` | Base trailing stop distance in pips (0 disables trailing). |
| `TrailingStepPips` | `5` | Minimum pip improvement before the trailing stop is advanced. |
| `CandleType` | `30m` time frame | Primary candle subscription processed by the strategy. |

## Implementation Notes

- Indicator buffers are cached internally so the strategy can access historical values for arbitrary shifts, replicating the MQL approach of indexing indicator arrays.
- The pip size is derived from `Security.PriceStep`. If the instrument does not report a valid price step the stop and trailing distances are treated as zero.
- All comments inside the code are written in English per repository guidelines.
- The Python version is intentionally omitted as requested.
