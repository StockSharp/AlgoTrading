# Reco RSI Grid Strategy

## Overview
This strategy reproduces the behaviour of the original MetaTrader "Reco" expert advisor using StockSharp's high level API. The algorithm opens an initial position based on the Relative Strength Index (RSI) and then places counter positions forming a grid. Distance between grid orders and their volume grow geometrically. All open positions are closed together when cumulative profit or loss reaches predefined thresholds.

## Trading Logic
- **Initial signal** – RSI exceeds the configured overbought or oversold zones. A short position is opened when RSI is above the sell level and a long position when it is below the buy level.
- **Grid expansion** – after the first order the strategy watches price movement against the last trade. When price moves by a calculated distance, an opposite market order is sent. The distance increases by *Distance Multiplier* on each new step and can be limited by *Max Distance* and *Min Distance*.
- **Volume scaling** – the size of every new order equals the initial *Lot* multiplied by *Lot Multiplier* raised to the count of already opened orders. Maximum and minimum volume limits are also supported.
- **Exit rules** – if *Use Close Profit* is enabled, all positions are closed when the aggregated profit is greater than *Profit First Order* multiplied by *Profit Multiplier* for each additional order. If *Use Close Lose* is enabled, the same logic is applied to losses using *Lose First Order* and *Lose Multiplier*.

## Parameters
| Name | Description |
|------|-------------|
| `RsiPeriod` | RSI indicator period. |
| `RsiSellZone` | RSI level that triggers a sell signal. |
| `RsiBuyZone` | RSI level that triggers a buy signal. |
| `StartDistance` | Initial distance from the last order expressed in points. |
| `DistanceMultiplier` | Multiplier applied to the distance for each additional order. |
| `MaxDistance` | Upper limit for distance growth (0 disables). |
| `MinDistance` | Lower limit for distance growth (0 disables). |
| `MaxOrders` | Maximum number of simultaneous open orders (0 means no limit). |
| `Lot` | Base order volume. |
| `LotMultiplier` | Multiplier for volume scaling. |
| `MaxLot` | Maximum allowed volume per order (0 disables). |
| `MinLot` | Minimum allowed volume per order (0 disables). |
| `UseCloseProfit` | Enable closing all positions by profit target. |
| `ProfitFirstOrder` | Profit target for the first order. |
| `ProfitMultiplier` | Profit multiplier for subsequent orders. |
| `UseCloseLose` | Enable closing all positions by loss threshold. |
| `LoseFirstOrder` | Loss threshold for the first order. |
| `LoseMultiplier` | Loss multiplier for subsequent orders. |
| `PointMultiplier` | Multiplier applied to the security price step to calculate one point. |
| `CandleType` | Type of candles used for indicator calculations. |

## Notes
- The strategy works with market orders and assumes immediate execution.
- Positions are netted: opening an opposite order may reduce or reverse the current position.
- The strategy uses tabs for indentation and English comments as required by project conventions.
