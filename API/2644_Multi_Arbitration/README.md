# Multi Arbitration Strategy

## Overview
The **Multi Arbitration Strategy** is a StockSharp port of the MetaTrader "Multi_arbitration 1.000" expert advisor. The original script continuously evaluates existing buy and sell positions, adds new trades in the direction with weaker floating profit, and performs a global liquidation once overall profit targets are met. This C# implementation keeps the core decision logic while adapting it to StockSharp's netting portfolio model and high-level strategy API.

The strategy:
- Opens an initial long position as soon as the first finished candle arrives.
- Compares the unrealized profit of the active direction with the alternative direction to decide whether a reversal is required.
- Forces a flat position when the configured profit target is exceeded or when position pressure grows beyond a configurable limit.
- Uses only market orders (`BuyMarket` / `SellMarket`) to maintain simplicity and fast execution.

## Trading Logic
1. **Initial order** – The very first finished candle triggers a long market order with the configured trade volume. This reproduces the MetaTrader expert's immediate market entry.
2. **Profit comparison** – On every finished candle the strategy calculates the floating PnL of the current direction:
   - Long profit = `(close - entry) * volume`
   - Short profit = `(entry - close) * volume`
3. **Position selection** – If the alternative direction would currently perform better than the active one, the strategy flips the position by sending a market order sized to cover the existing exposure and open a new position in the new direction. When no position is open, the algorithm defaults to a long entry, matching the original expert advisor.
4. **Position limit guard** – A configurable `MaxOpenPositions` parameter mirrors the MetaTrader check against `LimitOrders()`. When the combined long/short exposure reaches this cap and the strategy is profitable, it flattens the book to avoid over-leverage.
5. **Profit target exit** – When the account PnL (realized + unrealized) exceeds the `ProfitForClose` threshold the strategy closes all positions, exactly like the original `Equity - Balance` check.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `TradeVolume` | Volume used for every market order. Represents the minimum lot size in the original EA. | `1` |
| `ProfitForClose` | Profit threshold that triggers a global exit once exceeded. | `300` |
| `MaxOpenPositions` | Maximum number of simultaneous positions allowed before the strategy forces a flatten. Acts as `limit - 15` equivalent. | `15` |
| `CandleType` | Candle data type that synchronizes trade decisions. Default is 1-minute time frame. | `1 minute candles` |

## Implementation Notes
- StockSharp uses a netting position model, so the strategy can hold only one net direction at a time. Reversals are handled by sizing market orders to both close the existing exposure and open a new position in the opposite direction.
- The `StartProtection()` call is used to inherit built-in risk handling (e.g., stop-out on non-zero positions when the strategy is stopped).
- All state variables (`_entryPrice`, `_currentSide`, `_initialOrderPlaced`) are reset on `OnReseted` to support restarts and repeated simulations without stale data.
- The strategy only reacts to **finished candles** to avoid double-counting profits on partially formed bars.

## Usage Recommendations
- Align the `TradeVolume` parameter with the instrument's lot size or contract multiplier.
- The `ProfitForClose` value should be set using the same currency as the account PnL (e.g., USD for FX accounts).
- Increase or decrease `MaxOpenPositions` depending on how aggressively you want the strategy to accumulate exposure before forcing a flatten.
- Because the strategy always begins with a long trade, consider manually starting it when long entries are acceptable for the traded instrument.

## Differences from the MetaTrader Version
- MetaTrader's hedging mode allows simultaneous long and short positions, while this port operates in a netting environment. The decision logic still compares directional profitability, but only one net position is kept at any moment.
- Platform-specific checks (terminal trading permissions, filling type selection, account magic numbers) are replaced with StockSharp equivalents such as `StartProtection()` and candle subscriptions.
- Commented diagnostics from the MQL file are not reproduced; rely on StockSharp logging if runtime information is required.
