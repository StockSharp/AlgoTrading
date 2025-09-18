# RRS Tangled EA Strategy

## Overview
The **RRS Tangled EA Strategy** is a StockSharp port of the MetaTrader 4 expert advisor "RRS Tangled EA". The original system randomly chooses trade direction and symbol, while capping the number of simultaneous orders and protecting floating profit through trailing stops and strict risk limits. The converted version focuses on the currently selected instrument, reproducing the random entry, trailing, and risk management behaviour using the high-level StockSharp API.

## Core Logic
1. Subscribe to the configured candle series and wait for completed candles.
2. On each bar:
   - Update trailing stop levels for existing long and short baskets.
   - Check stop-loss and take-profit distances using candle highs and lows.
   - Evaluate the floating profit of all open entries; close everything if it breaches the money-at-risk threshold.
   - If trading is allowed, spread is acceptable, and the number of entries is below the limit, draw a random integer in `[0, 3]`.
   - Open a new long when the random value is `1`, or a new short when the value is `2`, using a random volume between the configured bounds.
3. Trailing stops follow the best bid/ask once price moves by the activation distance, locking in profits if price retraces by the trailing gap.
4. Risk management can work in fixed-money mode or as a percentage of the current account balance. When floating loss exceeds the configured amount, all positions are flattened immediately.

## Parameters
| Name | Description |
|------|-------------|
| `MinVolume` | Lower bound for the randomly generated trade volume. |
| `MaxVolume` | Upper bound for the random trade volume. |
| `TakeProfitPips` | Target distance in pips, applied to the average entry price of the basket. |
| `StopLossPips` | Protective stop distance in pips, measured from the average entry price. |
| `TrailingStartPips` | Profit distance needed before the trailing logic activates. |
| `TrailingGapPips` | Gap maintained between the trailing stop and the best bid/ask price. |
| `MaxSpreadPips` | Maximum allowed spread before opening a new random entry. |
| `MaxOpenTrades` | Maximum number of simultaneous entries across both directions. |
| `RiskManagementMode` | Switches between fixed-money and balance-percentage risk handling. |
| `RiskAmount` | Amount of risk (currency or percentage) monitored against floating PnL. |
| `TradeComment` | Optional comment for bookkeeping, kept for compatibility with the source EA. |
| `Notes` | Informational text displayed inside the strategy status string. |
| `CandleType` | Candle series used for decision making. |

## Differences from the MQL Version
- Trades are executed on the strategy's assigned instrument instead of randomly selecting symbols from the MetaTrader market watch. This keeps the implementation compatible with StockSharp's single-security strategies.
- Order management is performed on aggregated long/short baskets, mirroring how the original EA grouped positions with the same magic numbers.
- Spread control relies on the latest best bid/ask from the order book instead of MetaTrader's `MarketInfo` calls.

## Usage Notes
- Ensure that the connected broker or simulator provides both bid and ask quotes so that spread and trailing calculations remain accurate.
- Set `MinVolume` and `MaxVolume` within the instrument's allowed volume range. The strategy automatically snaps the random volume to the symbol's volume step and limits.
- The risk management logic closes *all* trades immediately once the floating loss exceeds the configured threshold; no new positions are opened until the next candle.
