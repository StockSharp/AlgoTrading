# DLM v1.4 Grid Strategy

## Overview
This strategy is a StockSharp port of Alejandro Galindo's MetaTrader 4 expert advisor "DLM v1.4". The original robot combines a Fisher Transform signal filter with a martingale-style averaging scheme that progressively builds a grid of positions whenever price moves against the last entry. The StockSharp version keeps the same money-management ideas while adapting the execution and protection logic to the high-level API (candle subscriptions, indicator bindings and market/limit helpers).

## Trading logic
- Analyse finished candles from the configured timeframe and compute two indicators: the Fisher Transform and an SMA-smoothing of the Fisher values.
- Determine the basket direction from the relative position of the two lines. When Fisher rises above the smoother the strategy prepares to buy; when it drops below the smoother it prepares to sell. The `ReverseSignals` flag inverts this interpretation.
- Open the first position immediately (market order) once a direction is available and automatic trading is enabled (`ManualTrading = false`).
- While the basket is active, keep adding new entries every time price moves `GridDistancePips` against the most recent execution. Depending on the `UseLimitOrders` flag the additional trades are either sent as market orders (at the next candle close) or as resting limit orders positioned exactly one grid step away from the last fill.
- The volume of each new trade follows the original martingale growth: multiply the base lot size by 1.5 when `MaxTrades > 12`, otherwise double the size. The base size itself can be fixed (`LotSize`) or derived from account equity when `UseMoneyManagement` is enabled.
- Every fill refreshes the aggregated stop-loss and take-profit levels so that the whole basket shares a single set of protective levels. Trailing-stop logic can tighten the stop after price travels `GridDistancePips + TrailingStopPips` in the profitable direction.

## Account protection
- **Secure profit guard** (`SecureProfitProtection`): once the number of open entries reaches `OrdersToProtect`, the unrealized profit (in account currency) is compared with `SecureProfit`. If the threshold is reached, the whole basket is closed immediately.
- **Equity protection** (`EquityProtection` + `EquityProtectionPercent`): monitors the current portfolio value and closes the basket whenever equity drops below the selected percentage of the equity captured at strategy start.
- **Money drawdown protection** (`AccountMoneyProtection` + `AccountMoneyProtectionValue`): stops trading when the currency drawdown from the initial equity exceeds the configured amount.
- **Lifetime protection** (`OrdersLifeSeconds`): enforces a maximum lifetime for the most recent entry; when the limit is exceeded, all trades are closed and the martingale cycle is halted.
- **Friday filter** (`TradeOnFriday`): prevents new baskets from starting on Fridays if disabled.

All protective exits use market orders to guarantee execution. Pending limit orders are cancelled whenever a protection block is triggered or when the grid is reset.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TakeProfitPips` | Shared take-profit distance (pips) applied to every entry. |
| `StopLossPips` | Initial stop-loss distance (pips) for each new trade. |
| `TrailingStopPips` | Trailing stop distance that becomes active after the trigger threshold. |
| `MaxTrades` | Maximum number of averaging steps allowed in the basket. |
| `GridDistancePips` | Minimum adverse move (pips) before adding the next order. |
| `LotSize` | Base lot size when money management is disabled. |
| `UseMoneyManagement` | Enables balance-based sizing via the original risk formula. |
| `RiskPercent` | Risk percentage used to derive the dynamic base lot size. |
| `AccountType` | Scaling applied to the dynamic lot size (0 standard, 1 mini, 2 micro). |
| `SecureProfitProtection` | Enables the floating-profit guard. |
| `SecureProfit` | Unrealized profit (currency units) required to trigger the guard. |
| `OrdersToProtect` | Minimum number of open entries before secure profit activates. |
| `EquityProtection` | Enables the equity-percentage safety net. |
| `EquityProtectionPercent` | Equity percentage threshold relative to the start of the strategy. |
| `AccountMoneyProtection` | Enables drawdown (currency) based protection. |
| `AccountMoneyProtectionValue` | Maximum tolerated drawdown in account currency. |
| `TradeOnFriday` | Allows/disallows opening new baskets on Fridays. |
| `OrdersLifeSeconds` | Maximum lifetime (seconds) for the latest order before liquidation. |
| `ReverseSignals` | Inverts the Fisher Transform direction. |
| `UseLimitOrders` | Switch between market and limit entries for averaging trades. |
| `ManualTrading` | Disables automatic entries when set to true. |
| `CandleType` | Timeframe used for the indicator calculations. |
| `FisherLength` | Lookback length for the Fisher Transform. |
| `SignalSmoothing` | SMA period applied to smooth Fisher values. |
| `DefaultPipValue` | Fallback pip value used to convert unrealized P/L into currency. |

## Notes
- All comments in the source code are in English as required by the repository guidelines.
- The strategy relies exclusively on the StockSharp high-level API (`SubscribeCandles`, `Bind`, `BuyLimit`, `SellLimit`, etc.) and does not manipulate indicator buffers directly.
- Money-management calculations reuse the original risk formula, but volume and price adjustments are passed through `Security.ShrinkVolume` and `Security.ShrinkPrice` to respect the instrument's contract specification.
- The conversion keeps the behaviour of the MetaTrader EA as close as possible while accounting for StockSharp differences (for example, basket exits use market orders instead of modifying existing orders).
