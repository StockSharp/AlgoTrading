# Freeman ATR MA RSI Grid Strategy

## Overview
This strategy replicates the MetaTrader "freeman" expert advisor using StockSharp's high level API. It layers multiple market positions while a trend measured by a moving average slope stays aligned with an RSI confirmation. Every entry and exit distance is defined in pips and converted to price units using the instrument tick size so the behaviour matches the original forex implementation.

## Trading Logic
1. Subscribe to a single candle series (configurable timeframe) and update the ATR, moving average and RSI indicators on each finished candle.
2. Generate a directional signal when:
   - The moving average slope is positive or negative by comparing the latest value with the previous bar (optional trend filter).
   - The price is far enough from the moving average to avoid entries directly on the line.
   - The RSI crosses the upper or lower threshold if the RSI filter is enabled. The MetaTrader logic is kept intact, including the quirk where an RSI sell confirmation returns `-11`, so activating both filters favours long trades only.
3. Respect the maximum number of simultaneously open positions. Additional entries in the same direction are allowed only when price has moved against the last fill by the configured pip distance, effectively building a grid.
4. Every entry uses ATR-based stop-loss and take-profit levels. Trailing stops tighten the protective stop once price moves by the trailing step plus trailing stop distance.
5. Exits are executed via opposite market orders when the candle range hits the stop, target or trailing level.

## Risk Management
- ATR multipliers control the fixed stop-loss and take-profit distances. Setting a multiplier to zero disables that protection.
- Trailing stops are optional and are defined by two pip parameters: the actual trailing distance and the extra step required before moving the stop again.
- The strategy relies on the base `Volume` property for sizing; no automated money management is applied beyond the position cap.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Timeframe used for indicator calculations. |
| `MaxPositions` | Maximum number of simultaneously open positions (sum of long and short). |
| `DistancePips` | Minimum pip distance between consecutive entries in the same direction. |
| `AtrPeriod` | Averaging period for the ATR indicator. |
| `AtrStopLossMultiplier` | ATR multiplier for the protective stop. `0` disables the stop. |
| `AtrTakeProfitMultiplier` | ATR multiplier for the profit target. `0` disables the target. |
| `UseTrendFilter` | Enables the moving average slope filter. |
| `DistanceFromMaPips` | Minimum pip distance between price and the moving average when the trend filter is active. |
| `MaPeriod`, `MaShift`, `MaMethod`, `MaPriceType` | Moving average parameters mirroring the MetaTrader inputs. |
| `UseRsiFilter` | Enables the RSI confirmation filter. |
| `RsiLevelUp`, `RsiLevelDown`, `RsiPeriod`, `RsiPriceType` | RSI configuration with applied price selection. |
| `TrailingStopPips`, `TrailingStepPips` | Trailing stop distance and step measured in pips. |
| `CurrentBarOffset` | Offset applied when reading indicator values, emulating the `CurrentBar` input from the expert advisor. |

## Notes
- Pip conversion multiplies the instrument `PriceStep` by 10 when the instrument has 3 or 5 decimal places to reproduce MetaTrader's point-to-pip adjustment.
- The strategy uses a netting position model; opposite signals close existing positions before opening trades in the new direction.
- Start protection is enabled at launch to guard against unexpected reconnections before any trades are placed.
