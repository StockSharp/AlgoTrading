# Mean Reversion Donchian Strategy

## Overview
This strategy is a port of the MetaTrader expert advisor `MeanReversion.mq5`. It trades a simple mean-reversion pattern: whenever price prints a fresh low within the selected lookback window the strategy opens a long position, targeting the midpoint of the recent range. When a new high appears the strategy mirrors the logic on the short side. Position size is calculated from the risk percentage and the stop distance, closely replicating the lot calculation that the original EA performs.

## Trading Logic
1. Build a Donchian Channel using the configured candle type and lookback period. The upper band marks the highest high, and the lower band the lowest low over the window. The midpoint `(upper + lower) / 2` acts as the mean reversion target.
2. If the current finished candle makes a new low (`Low <= LowerBand`) and no position is open, the strategy buys at market. The protective stop is reflected around the entry price so that the midpoint becomes the profit target, matching the MetaTrader computation `sl = 2 * Ask - tp`.
3. If the candle makes a new high (`High >= UpperBand`) and no position is open, the strategy sells at market with a symmetric stop above price. The midpoint again acts as the take-profit level.
4. The stop-loss and take-profit are monitored on every finished candle. A breakout beyond the stop closes the position immediately, while touching the midpoint exits the trade at the intended target. The internal state resets automatically whenever the position is flat.

## Position Sizing
* Risk per trade equals `Portfolio.CurrentValue * (RiskPercent / 100)`. If portfolio data is not available the strategy falls back to the minimal tradable volume.
* Contract risk is measured as `|EntryPrice - StopPrice|`. The raw volume is `RiskAmount / perUnitRisk` and is normalized to the instrument volume step. Minimum and maximum exchange constraints are respected. When the normalized volume is smaller than the minimal tradable size, the minimum is used instead.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle type and timeframe used for building the Donchian channel. | 15-minute time frame |
| `LookbackPeriod` | Number of candles used to compute the highest high and lowest low. | 200 |
| `RiskPercent` | Percentage of portfolio equity risked per trade. | 1% |

All parameters support optimization through the built-in optimizer.

## Additional Notes
* The strategy only trades one position at a time, replicating the `PositionsTotal()>0` guard from the MQL version.
* Stop-loss and take-profit prices are maintained internally instead of sending separate orders, which keeps the logic close to the original Expert Advisor while remaining compatible with the high-level API.
* When portfolio equity or instrument volume information is missing the strategy still trades using the smallest possible volume to keep behaviour deterministic.
