# Two MA Four Level Bands Strategy

## Overview
This strategy recreates the MetaTrader expert advisor `ytg_2MA_4Level`. It compares a fast moving average with a slower one and triggers entries when the fast curve crosses the slow curve either directly or within four configurable offset bands. Positions are protected by symmetric stop-loss and take-profit distances expressed in pips, just like in the original implementation.

## Signal logic
1. Two moving averages are calculated on the selected candle series. Both the averaging method (SMA, EMA, SMMA, LWMA) and the applied price can be tuned independently for the fast and slow lines.
2. On every finished candle the strategy samples the moving averages `CalculationBar` bars back (default `1`) and also one bar earlier. This mirrors the MetaTrader `iMA(..., shift)` call and ensures that only closed candles generate trades.
3. A **buy** signal fires when the fast average crosses above the slow one, or when the crossover happens above/below the slow average shifted by `UpperLevel1`, `UpperLevel2`, `LowerLevel1`, or `LowerLevel2` pips.
4. A **sell** signal uses the mirrored conditions with the fast average crossing below the slow line (and the same four offset bands).
5. The strategy only opens a new market position when no orders are active and the current position is flat, matching the single-ticket behaviour of the MQL expert.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `TakeProfitPips` | `int` | `130` | Take-profit distance in pips. Set to `0` to disable the target. |
| `StopLossPips` | `int` | `1000` | Stop-loss distance in pips. Set to `0` to disable the protective stop. |
| `TradeVolume` | `decimal` | `1` | Base lot size sent with each order (auto-adjusted to `VolumeStep`). |
| `CalculationBar` | `int` | `1` | Number of bars used as the anchor for the MA comparison (MetaTrader `shift`). |
| `FastPeriod` / `SlowPeriod` | `int` | `14` / `180` | Period lengths of the moving averages. |
| `FastMethod` / `SlowMethod` | `MovingAverageMethod` | `Smoothed` | Averaging technique: `Simple`, `Exponential`, `Smoothed`, or `LinearWeighted`. |
| `FastPrice` / `SlowPrice` | `CandlePrice` | `Median` | Applied price used by each moving average. |
| `UpperLevel1` / `UpperLevel2` | `int` | `500` / `250` | Positive offsets (in pips) added to the slow MA for tolerance checks. |
| `LowerLevel1` / `LowerLevel2` | `int` | `500` / `250` | Negative offsets (in pips) subtracted from the slow MA for tolerance checks. |
| `CandleType` | `DataType` | `15m` time frame | Candle series on which the indicators operate. |

## Implementation notes
- Stop-loss and take-profit orders are emulated through `StartProtection` with distances converted from pips to price units using the instrument’s `PriceStep`. Five-digit FX quotes automatically receive the MetaTrader-style `*10` multiplier.
- Internal queues store only the data needed to reproduce the `shift` logic; no full candle history is accumulated.
- Orders are issued with `BuyMarket` / `SellMarket` and inherit the normalized volume so that the UI reflects the active lot size.
- Chart output draws the candle series together with both moving averages and executed trades for quick visual inspection.
- All inline comments are in English to comply with the project guidelines.

## Usage tips
- Pick the same candle interval that you would use in MetaTrader; the default `15`-minute series can be changed via `CandleType`.
- Reduce the offset levels to make the signals more selective, or enlarge them to accept wider “near miss” crossovers.
- Setting `CalculationBar` to `0` makes the strategy react to the latest closed candle (no lag), while higher values move the trigger further into the past for additional confirmation.
- Disable the protective legs (`StopLossPips = 0`, `TakeProfitPips = 0`) if the exits should be managed manually or by another module.
