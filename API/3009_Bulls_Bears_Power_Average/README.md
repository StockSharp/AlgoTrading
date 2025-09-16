# Bulls & Bears Power Average Strategy

## Overview
- Port of the MetaTrader 5 expert `MySystem.mq5` located in `MQL/22016`.
- Detects momentum reversals by averaging Elder Bulls Power and Bears Power values computed from candle extremes and an EMA.
- Enters **long** when the average increases while still below zero (bearish pressure is fading) and **short** when the average decreases while still above zero (bullish pressure is fading).
- Designed for one open position at a time; stop-loss and take-profit are optional and expressed in pips.

## Indicator Logic
| Component | Description |
|-----------|-------------|
| Exponential Moving Average (EMA) | Applied to candle close prices. The `MaPeriod` parameter controls the smoothing window (default 5). |
| Bulls Power (derived) | Calculated as `High - EMA`. Measures bullish strength relative to the EMA. |
| Bears Power (derived) | Calculated as `Low - EMA`. Measures bearish strength relative to the EMA. |
| Average Power | `(Bulls Power + Bears Power) / 2`. This synthetic oscillator is compared with its previous value to detect changes in momentum. |

The strategy evaluates the last two finished candles. New trades are only evaluated when a candle is fully completed to avoid intrabar noise.

## Entry Rules
1. Wait for the EMA to be fully formed (i.e., it processed at least `MaPeriod` candles).
2. Compute Bulls Power and Bears Power for the just-closed candle using its high/low and the EMA value.
3. Average both forces to obtain the current oscillator reading.
4. Compare with the previous average:
   - **Long setup**: previous average `<` current average **and** current average `< 0`. Enter long if there is no existing position.
   - **Short setup**: previous average `>` current average **and** current average `> 0`. Enter short if flat.
5. Once in a trade, rely on optional protective orders or manual management. The algorithm does not generate exit signals besides stop-loss/take-profit.

## Risk Management
- `StopLossPips`: Optional absolute stop distance in pips (0 disables the stop). Converted using the instrument `PriceStep`.
- `TakeProfitPips`: Optional absolute profit target in pips (0 disables the target).
- Protective orders are registered as soon as the position opens via `StartProtection` with market execution.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `OrderVolume` | 0.1 | Order size for market entries. |
| `StopLossPips` | 15 | Stop-loss distance in pips. Set to `0` to disable. |
| `TakeProfitPips` | 95 | Take-profit distance in pips. Set to `0` to disable. |
| `MaPeriod` | 5 | EMA length used for the Bulls/Bears Power calculation. |
| `CandleType` | 1-hour time frame | Candle series used for all computations (change to match your data feed). |

## Usage Notes
1. Attach the strategy to an instrument and ensure the `CandleType` matches the intended time frame.
2. Adjust `OrderVolume`, `StopLossPips`, and `TakeProfitPips` to match brokerage requirements.
3. Run the strategy; it automatically subscribes to candles, updates the EMA, and issues market orders on qualifying signals.
4. Only one position can exist at a time. When a trade is active, new signals are ignored until the protective orders close the position or it is closed manually.
5. Because the original MQL version used `InpBarCurrent = 1`, this port always works on fully closed candles and compares consecutive values; no intrabar recalculation is performed.

## Differences vs. Original MQL Expert
- Uses StockSharp high-level `Strategy` API with candle subscriptions and indicator binding instead of manual buffer access.
- Automatically derives pips from `PriceStep` instead of manual digit adjustments.
- Skips the original commented-out order management and relies on built-in position protection.
- Keeps the single-position constraint of the source by ignoring signals while a position exists.

## Testing Recommendations
- Backtest on the intended symbol/time frame with historical data that includes high/low prices for accurate Bulls/Bears computation.
- Validate protective order behaviour with your broker's tick size and volume step before running live.
- Experiment with different `MaPeriod` values to adapt sensitivity to the instrument volatility.
