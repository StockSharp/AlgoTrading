# Rsi Test Strategy

## Overview
`RsiTestStrategy` converts the MetaTrader 4 expert advisor **RSI_Test** into StockSharp's high level API. The strategy combines a fast RSI momentum filter with simple candle confirmation and risk-aware position sizing. It trades a single instrument defined by the host strategy and uses completed candles only, mirroring the tick-to-close logic of the original code.

## Trading Rules
1. Calculate the Relative Strength Index with the configurable `RsiPeriod`.
2. Go long when the RSI is rising from an oversold region (`BuyLevel`) *and* the current candle opens above the previous one.
3. Go short when the RSI is falling from an overbought region (`SellLevel`) *and* the current candle opens below the previous one.
4. Respect the `MaxOpenPositions` limit. A value of `0` disables the cap; otherwise the net exposure cannot exceed `MaxOpenPositions * Volume`.
5. Manage exits through a stair-style trailing stop that activates once price advances by `TrailingDistanceSteps` ticks beyond the average entry price.
6. No explicit take-profit is used. Positions exit when the trailing stop is triggered or when the trading session terminates the strategy.

## Position Sizing and Risk
* The strategy derives a tentative order size from `RiskPercentage` of the portfolio's current value. When the instrument provides margin data (`Security.MarginBuy`/`Security.MarginSell`) the required capital per lot is honoured; otherwise the amount is divided by the latest close price as a conservative fallback.
* Volumes are rounded to `Security.VolumeStep` (or two decimals if the step is unknown) and clamped inside the `Security.MinVolume`/`Security.MaxVolume` range.
* Set `RiskPercentage` to zero to disable dynamic sizing and always trade the configured `Volume`.

## Trailing Stop Behaviour
* `TrailingDistanceSteps` expresses the distance in price steps (`Security.PriceStep`). If the instrument does not expose a step, the distance is treated as a direct price offset.
* Once the close or intrabar high crosses the activation level (`entry + distance` for longs, `entry - distance` for shorts) the strategy arms the trailing stop at the same offset beyond the entry price.
* The protective stop is applied only once per position, exactly like the original EA that moves the stop from break-even to the first stair and keeps it there.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `RsiPeriod` | RSI lookback period. | `14` |
| `BuyLevel` | Oversold threshold that prepares a long setup. | `12` |
| `SellLevel` | Overbought threshold that prepares a short setup. | `88` |
| `RiskPercentage` | Portfolio share used for position sizing. Set `0` to ignore. | `10` |
| `TrailingDistanceSteps` | Distance (in price steps) required to arm the trailing stop. | `50` |
| `MaxOpenPositions` | Maximum simultaneous positions; `0` removes the limit. | `1` |
| `CandleType` | Primary timeframe for calculations. | `15` minutes |
| `Volume` | Base volume when risk sizing cannot be resolved. | `1` |

## Usage Notes
1. Attach the strategy to a security that exposes accurate `PriceStep`, `VolumeStep`, and margin metadata for the best match with the MQL behaviour.
2. The algorithm checks only completed candles (`CandleStates.Finished`), so backtests should use the same timeframe as production.
3. `StartProtection()` from the base class is enabled in `OnStarted`, allowing StockSharp's built-in risk control to manage unexpected position remnants.
4. Because the original expert advisor launched MetaTrader optimizations through `GlobalVariableGet`, that behaviour is intentionally omitted. Configure the parameters directly within StockSharp.
5. Combine the strategy with a portfolio that updates `Portfolio.CurrentValue` for dynamic risk sizing. Without it the strategy gracefully falls back to the static `Volume`.
