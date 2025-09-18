# Terminator Strategy

## Overview

The Terminator strategy reproduces the grid-based martingale logic from the MetaTrader 4 expert advisor "Terminator v2.0" using the StockSharp high level API. The strategy enters in the direction of the MACD slope and then builds an averaging basket whenever price moves against the position by a configurable number of pips. The basket is managed with optional stop-loss, take-profit, trailing stop and a secure-profit protection rule that can close the last trade when floating profit reaches a target.

## Trading Logic

1. **Signal generation** – On each finished candle the strategy evaluates the MACD histogram. When the MACD value increases compared to the previous value a bullish bias is assumed, while a decreasing MACD indicates a bearish bias. A `ReverseSignals` flag can invert the interpretation.
2. **Initial entry** – If there are no open trades and the schedule filter (`StartYear`, `StartMonth`, `EndYear`, `EndMonth`) allows trading, the strategy submits a market order in the detected direction unless `ManualTrading` is enabled.
3. **Martingale averaging** – When there is an open basket the strategy waits for price to move adversely by `EntryDistancePips`. Every additional entry doubles the previous volume (or multiplies it by 1.5 if `MaxTrades` is greater than 12) up to the `MaxTrades` limit. Position size can also be derived from account balance by enabling `UseMoneyManagement`.
4. **Risk management** –
   - **Take-profit**: `TakeProfitPips` defines the distance used to position the shared take-profit level.
   - **Initial stop**: `InitialStopPips` optionally sets the initial protective stop for the complete basket.
   - **Trailing stop**: `TrailingStopPips` activates after the basket gains at least the trailing distance plus one spacing step, and then moves the stop in the trade direction.
   - **Account protection**: when `UseAccountProtection` is enabled and the number of open trades reaches `MaxTrades - OrdersToProtect`, the floating profit is compared against `SecureProfit` (or the current portfolio value if `ProtectUsingBalance` is true). If the threshold is exceeded the last trade is closed to lock in gains and no new entries are allowed until the basket is reset.
5. **Basket reset** – When the net position returns to zero all internal counters are cleared, allowing a new trading cycle.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TakeProfitPips` | Distance in pips for the basket take-profit level. |
| `InitialStopPips` | Initial stop distance in pips. Set to zero to disable. |
| `TrailingStopPips` | Trailing stop distance in pips. Set to zero to disable. |
| `MaxTrades` | Maximum number of martingale entries allowed simultaneously. |
| `EntryDistancePips` | Minimum adverse move required before adding the next trade. |
| `SecureProfit` | Floating profit threshold used by the protection module. |
| `UseAccountProtection` | Enables the secure-profit protection block. |
| `ProtectUsingBalance` | When true the protection threshold equals the current portfolio value instead of `SecureProfit`. |
| `OrdersToProtect` | Number of final trades watched by the protection block (mirrors the original "Orders to Protect" input). |
| `ReverseSignals` | Inverts bullish and bearish MACD signals. |
| `ManualTrading` | Disables automatic entries while keeping basket management active. |
| `LotSize` | Fixed lot size when money management is disabled. |
| `UseMoneyManagement` | Enables balance-based sizing derived from `RiskPercent`. |
| `RiskPercent` | Risk percentage (per 100%) applied when money management is active. |
| `IsStandardAccount` | Toggles between standard and mini lot scaling. |
| `EurUsdPipValue`, `GbpUsdPipValue`, `UsdChfPipValue`, `UsdJpyPipValue`, `DefaultPipValue` | Pip value assumptions used to convert pips to currency for the protection rule. |
| `StartYear`, `StartMonth`, `EndYear`, `EndMonth` | Restrict the time window when new baskets can be opened. |
| `CandleType` | Timeframe used to build the MACD signal. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Period settings of the MACD indicator. |

## Usage Notes

- The strategy subscribes to the candle type defined by `CandleType` and only reacts to finished candles.
- To mirror the original MT4 behaviour make sure the symbol pip value parameters match your broker specifications.
- When `ManualTrading` is enabled you can still manage orders manually; the algorithm will continue trailing stops and enforcing account protection on the open basket.
- The implementation focuses on the MACD-based entry method of the original expert advisor because the other modes relied on custom indicators that are not available in StockSharp.

## Conversion Details

- Money management, pip spacing, martingale scaling and secure-profit logic follow the original MQ4 code structure.
- The MT4 `AccountProtection` and `AllSymbolsProtect` options are combined into `UseAccountProtection` and `ProtectUsingBalance` parameters.
- `ReverseCondition` and `Manual` flags from the source map to `ReverseSignals` and `ManualTrading` respectively.
- Stop-loss and trailing rules operate on the aggregate basket rather than per order, similar to the source expert advisor behaviour.

## How to Run

1. Open the solution in Visual Studio.
2. Add the strategy to a `StrategyRunner` or `StrategyConnector` instance.
3. Configure the parameters in the UI or via code.
4. Start the strategy; it will automatically subscribe to the specified candle series and begin evaluating signals.
