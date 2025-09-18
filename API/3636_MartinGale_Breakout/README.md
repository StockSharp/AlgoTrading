# MartinGale Breakout Strategy

## Overview
The **MartinGale Breakout Strategy** is a breakout-following system converted from the MetaTrader 4 expert advisor *MartinGaleBreakout*. The original robot enters positions after detecting abnormally large candles and applies a martingale-style recovery mechanism to regain previous losses. This StockSharp port reproduces the behaviour using the high-level strategy API with candle subscriptions and money-management parameters.

The strategy monitors a configurable candle series, looking for candles whose range is at least three times greater than the average range of the previous ten bars. When such a candle closes strongly in one direction, the strategy opens a market position in that direction. If the position is closed with a loss that exceeds a configurable threshold, the recovery mode increases the take-profit distance to compensate for the realised drawdown.

## Trading Logic
1. Subscribe to the selected candle series (15-minute candles by default).
2. Maintain the most recent 11 finished candles to evaluate abnormal volatility.
3. Detect a bullish breakout when:
   - The current candle is three times larger than the average range of the previous ten candles.
   - The candle closes in the upper half of its range.
4. Detect a bearish breakout using the symmetric conditions.
5. Open a market position in the breakout direction if:
   - No other position is currently open.
   - The estimated capital exposure is below the configured balance percentage.
6. Close positions and reset profit/loss targets when:
   - Floating profit reaches the take-profit threshold.
   - Floating loss reaches the stop-loss threshold.
7. When a stop-loss occurs, switch to recovery mode:
   - Increase the take-profit distance by the configured multiplier.
   - Expand the stop-loss limit to the maximum allowed percentage.
   - Continue trading until the next target is reached, then reset to the base configuration.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `TakeProfitPoints` | Base take-profit distance expressed in instrument points. | `50` |
| `BalancePercentageAvailable` | Maximum share of the account balance that can be allocated to a single trade. | `50%` |
| `TakeProfitBalancePercent` | Target profit as a percentage of account balance. | `0.1%` |
| `StopLossBalancePercent` | Maximum drawdown before triggering recovery. | `10%` |
| `StartRecoveryFactor` | Portion of the stop-loss used before activating recovery mode. | `0.2` |
| `TakeProfitPointsMultiplier` | Multiplier applied to the take-profit distance while recovering. | `1` |
| `CandleType` | Candle series used for breakout calculations. | `15-minute` |

## Position Sizing and Risk Control
- The strategy calculates the required volume to achieve the configured monetary take-profit using the instrument tick size and tick value.
- Volumes are normalised to exchange constraints (step, minimum, maximum).
- Estimated capital exposure must not exceed the configured balance percentage.
- Recovery mode dynamically expands the take-profit target after a loss, emulating the original martingale behaviour while keeping positions limited to a single open trade.

## Notes
- The strategy relies on portfolio balance information; initialise it with a portfolio connection before starting.
- Commission handling mirrors the original EA by focusing on floating P&L derived from the current position.
- No pending orders are used—entries and exits are performed with market orders only.
