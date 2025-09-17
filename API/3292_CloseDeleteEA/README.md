# CloseDeleteEA Strategy

## Overview
The CloseDeleteEA strategy reproduces the MetaTrader utility that mass-closes positions and removes pending orders.
It periodically scans the selected portfolio and sends market orders or cancel requests according to user-defined filters.
This makes it useful for emergency liquidation or clean-up scenarios when manual order management is too slow.

## Key Features
- Closes long and/or short exposure with market orders.
- Cancels pending orders that match the configured filters.
- Optional profit/loss filters to avoid touching specific positions.
- Restricts the scan to the current security or processes the entire portfolio.
- Filters positions and orders by strategy identifier.

## Parameters
| Name | Description |
| --- | --- |
| `CloseBuyPositions` | Close long exposure that matches the filters. |
| `CloseSellPositions` | Close short exposure that matches the filters. |
| `CloseMarketPositions` | Enables the market-position closing module. |
| `CancelPendingOrders` | Enables canceling of pending orders. |
| `CloseOnlyProfitable` | Close positions only when the current PnL is non-negative. |
| `CloseOnlyLosing` | Close positions only when the current PnL is non-positive. |
| `ApplyToCurrentSecurity` | When true, only the strategy security is scanned. Otherwise all securities in the portfolio are processed. |
| `TargetStrategyId` | Optional strategy identifier filter (empty value matches everything). |
| `TimerInterval` | Timer frequency used for the management loop. |

## Usage Notes
1. Attach the strategy to a connector with an assigned portfolio.
2. Optionally configure filters before starting the strategy.
3. Start the strategy to trigger the close/delete cycle. The strategy stops automatically once no matching positions or orders remain.
4. Keep in mind that cancel requests can only target orders that are visible to the strategy through the connector.

## Differences vs. MQL Version
- StockSharp works with aggregated positions, so individual ticket-level control is replaced with volume-based net exposure management.
- Strategy-id filtering mimics the original magic-number concept.
- Chart-cleaning visual elements from MetaTrader are not reproduced.
