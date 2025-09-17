# Ten Points 3 v005 Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 4 expert advisor "10points 3 v005". It follows the MACD slope to decide whether the current averaging basket should be long or short and keeps opening martingale orders every time price moves against the active position by a configurable distance. The enhanced "v005" release adds equity based protection rules, day and time filters and the option to disable either the long or the short cycle.

## Trading logic
- Read the direction from the MACD main line. When the indicator rises the next basket will be long, when it falls the basket will be short. An option allows reversing the interpretation.
- Open the first market position immediately once a direction exists. Subsequent entries are added whenever price moves `EntryDistancePips` against the floating position.
- Order sizes grow geometrically. The multiplier is controlled by `MartingaleFactor` (or `HighTradeFactor` when more than 12 trades are allowed). Volumes are aligned to the instrument volume step and capped at 100 lots.
- Every entry updates aggregated stop-loss and take-profit levels. Initial values are offset by `InitialStopPips` and `TakeProfitPips`, while trailing logic activates after the position earns `EntryDistancePips + TrailingStopPips` in favor.
- If account protection is enabled the strategy can align the target with the best entry (`ReboundLock`) and close the most recent order once floating profit reaches `SecureProfit`.
- Equity protection rules close the whole basket when the floating loss exceeds `StopLossAmount`, when equity rises above `ProfitTarget + ProfitBuffer`, or when equity drops below `StartProtectionLevel`.
- Trading is limited to the `OpenHour`/`CloseHour` window and is disabled completely on Fridays by default.

## Money management
When `UseMoneyManagement` is disabled the first order uses the fixed `LotSize`. When the flag is enabled the base volume is calculated from the current portfolio value and the `RiskPercent` parameter. Mini-account scaling can be simulated through `IsStandardAccount`.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TakeProfitPips` | Distance (in pips) of the take-profit applied to each entry. |
| `LotSize` | Base lot size when money management is disabled. |
| `InitialStopPips` | Initial stop-loss distance for every order. |
| `TrailingStopPips` | Trailing stop distance once the trigger threshold is reached. |
| `MaxTrades` | Maximum number of simultaneous martingale entries. |
| `EntryDistancePips` | Minimum adverse move required to add the next order. |
| `SecureProfit` | Floating profit (in currency units) required to trigger the account-protection exit. |
| `UseAccountProtection` | Enables the secure-profit and rebound lock logic. |
| `OrdersToProtect` | Number of final martingale steps protected by the secure-profit rule. |
| `ReverseSignals` | Reverses the MACD slope interpretation. |
| `UseMoneyManagement` | Enables balance-based sizing. |
| `RiskPercent` | Risk percentage used by the money-management formula. |
| `IsStandardAccount` | Uses standard-lot scaling instead of mini scaling. |
| `EurUsdPipValue`, `GbpUsdPipValue`, `UsdChfPipValue`, `UsdJpyPipValue`, `DefaultPipValue` | Pip values used to translate floating profit into currency. |
| `CandleType` | Candle timeframe used for signal generation. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD configuration. |
| `EnableLong`, `EnableShort` | Enable or disable the long/short basket. |
| `OpenHour`, `CloseHour`, `MinuteToStop` | Trading window configuration. |
| `StopLossProtection`, `StopLossAmount` | Equity-based stop-loss guard. |
| `ProfitTargetEnabled`, `ProfitTarget`, `ProfitBuffer` | Equity-based profit lock. |
| `StartProtectionEnabled`, `StartProtectionLevel` | Equity floor guard. |
| `ReboundLock` | Aligns exits with the best entry when protection is active. |
| `MartingaleFactor`, `HighTradeFactor` | Martingale multipliers. |
| `CloseOnFriday` | Disables trading during Fridays. |

## Notes
- The strategy uses the high-level StockSharp API (`SubscribeCandles` + `BindEx`) and does not expose raw indicator buffers.
- Every equity guard closes the active basket using market orders to replicate the original EA behaviour.
- Always validate the parameter values, pip size and pip value against your broker specifications before using the strategy in production.
