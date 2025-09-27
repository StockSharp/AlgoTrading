# Martingail Expert Strategy

## Overview
Martingail Expert is a trend-following martingale strategy that relies on the Stochastic Oscillator to time new sequences of trades. Once the indicator generates a direction, the strategy starts a ladder of market orders and manages the exposure using a dynamic profit target and a geometric position sizing scheme.

## Trading Logic
- Calculate a Stochastic Oscillator on the configured candle series. The most recent final values of %K and %D are cached for decision making.
- Start a new long sequence when `%K (previous) > %D (previous)` and `%D (previous)` is above the `BuyLevel` threshold.
- Start a new short sequence when `%K (previous) < %D (previous)` and `%D (previous)` is below the `SellLevel` threshold.
- After entering a sequence, every favorable price move equal to `ProfitFactor × openOrders` pips adds a new position with the base volume.
- Every adverse move of `StepPoints` pips multiplies the last filled volume by `Multiplier` and sends an averaging order in the same direction.

## Exit Rules
- Close the entire position as soon as the last fill price reaches a dynamic profit target at `ProfitFactor × openOrders` pips in the favorable direction.
- Reset the martingale state whenever the aggregated position size returns to zero.

## Risk Management
The martingale progression increases exposure quickly when price moves against the position. Adjust `Multiplier`, `StepPoints`, and `ProfitFactor` carefully to match the account size and instrument volatility.

## Parameters
| Name | Description |
| --- | --- |
| `Volume` | Base market order volume used for the first trade and every favorable add-on. |
| `Multiplier` | Factor applied to the last executed volume when averaging during adverse moves. |
| `StepPoints` | Distance in points that triggers a martingale averaging order. |
| `ProfitFactor` | Profit target per open order expressed in points. The actual distance is `ProfitFactor × number_of_orders`. |
| `KPeriod` | Lookback length for the %K line. |
| `DPeriod` | Smoothing length for the %D line. |
| `Slowing` | Additional smoothing applied to %K before comparing with %D. |
| `BuyLevel` | Minimum %D value required to allow a new long sequence. |
| `SellLevel` | Maximum %D value required to allow a new short sequence. |
| `CandleType` | Candle series used for calculations (default: 5-minute timeframe). |

## Notes
- Works best on liquid FX pairs where pip size and volume step allow granular scaling.
- Requires sufficient margin to withstand several martingale steps.
