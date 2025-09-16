# Ten Points 3 Strategy

## Overview
This strategy is a StockSharp port of the classic MetaTrader 4 expert advisor "10 points 3". It implements a martingale-style averaging system that follows the slope of the MACD histogram to decide whether the current cycle should be long or short. Once a direction is chosen, the strategy progressively adds new positions whenever price moves against the open trade by a configurable distance. The averaging process continues until either the profit target or the stop/protection rules are triggered.

## Trading logic
- Determine trade direction from the MACD main line: if the latest value is rising the strategy prepares to buy, otherwise it prepares to sell. An option allows reversing the interpretation.
- Open the first position immediately once a direction is available and the trading calendar (start/end year and month) allows new entries.
- While the position remains open, every adverse move of `EntryDistancePips` adds a new martingale order whose size is multiplied by 1.5 (when `MaxTrades` is above 12) or by 2 (otherwise).
- Each new order inherits an initial stop loss and take profit located `InitialStopPips` and `TakeProfitPips` away from the entry price. Aggregated protective levels are tracked for the whole position.
- A trailing stop activates after price travels `TrailingStopPips + EntryDistancePips` in profit and then follows the market with a distance of `TrailingStopPips`.
- When the number of active entries reaches `MaxTrades - OrdersToProtect`, the account-protection block checks the floating profit (converted to currency using symbol-specific pip values). If the profit exceeds `SecureProfit`, the last order is closed to reduce risk and no new trades are opened until the basket is flat again.

## Money management
- The base order size can be fixed (`LotSize`) or computed dynamically via the original balance-based formula. When `UseMoneyManagement` is enabled, the strategy uses the current portfolio value and the `RiskPercent` parameter to compute the base lot size. Mini accounts can be simulated by disabling `IsStandardAccount`.
- The calculated volume is capped at 100 lots before any martingale multipliers are applied.

## Order management
- All entries are executed as market orders (`BuyMarket`/`SellMarket`).
- Protective exits use market orders as soon as the calculated stop loss, trailing stop or take profit levels are reached.
- Account protection closes only the most recent order (`_lastEntryVolume`), matching the behaviour of the original expert.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TakeProfitPips` | Take profit distance applied to every entry. |
| `LotSize` | Fixed lot size when money management is disabled. |
| `InitialStopPips` | Initial stop loss distance for each order. |
| `TrailingStopPips` | Trailing stop distance after the trigger threshold is reached. |
| `MaxTrades` | Maximum number of averaging orders in the basket. |
| `EntryDistancePips` | Minimum adverse move before adding the next order. |
| `SecureProfit` | Floating profit (in currency units) required to trigger account protection. |
| `UseAccountProtection` | Enables the secure profit logic. |
| `OrdersToProtect` | Number of final martingale steps protected by the secure profit rule. |
| `ReverseSignals` | Reverses the MACD slope interpretation. |
| `UseMoneyManagement` | Enables balance-based sizing. |
| `RiskPercent` | Risk percentage used by the money-management formula. |
| `IsStandardAccount` | Uses standard-lot scaling instead of mini-account scaling. |
| `EurUsdPipValue`, `GbpUsdPipValue`, `UsdChfPipValue`, `UsdJpyPipValue`, `DefaultPipValue` | Monetary pip values used when computing secure profit. |
| `StartYear`, `StartMonth`, `EndYear`, `EndMonth` | Trading calendar that controls when new baskets may start. |
| `CandleType` | Timeframe used for MACD calculation and trade management. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD settings that drive direction detection. |

## Notes
- The implementation keeps the martingale multipliers and the protection logic of the original EA while using StockSharp high-level APIs (`SubscribeCandles` + `BindEx`).
- Floating-profit evaluation uses aggregated position data. Exact order-by-order results can differ slightly from MetaTrader because StockSharp closes baskets with market orders instead of modifying individual stop/limit orders.
- Always validate the parameters with your broker's contract specifications (pip size, pip value and lot scaling) before running the strategy on a live account.
