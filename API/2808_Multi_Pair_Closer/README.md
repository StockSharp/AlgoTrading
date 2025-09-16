# Multi Pair Closer Strategy

## Overview

The **Multi Pair Closer Strategy** mirrors the original MetaTrader script that supervises a basket of currency pairs and liquidates every open position once the combined floating profit hits a target or the accumulated loss breaches a safety threshold. The conversion leverages StockSharp's high-level API to track profits, enforce a minimum holding time, and close positions across several securities in one action.

## Logic

1. Resolve the watched instruments from the comma-separated `WatchedSymbols` parameter. If the list is empty, the main `Security` is used.
2. Subscribe to the selected candle type (default: 1-minute time frame) for each instrument. Every finished candle triggers a profit evaluation.
3. For each instrument the strategy stores:
   - The last computed profit (`Positions[i].PnL`).
   - The timestamp when a position first became non-zero to respect the `MinAgeSeconds` requirement.
4. After each update the net profit across all watched symbols is calculated:
   - If `ProfitTarget` is reached, all positions older than the minimum age are flattened using `BuyMarket` / `SellMarket` orders.
   - If the net profit drops below `-MaxLoss`, the same liquidation logic is applied as a protective stop.
5. Detailed logs summarise the profit per instrument and the current basket result after every evaluation.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `WatchedSymbols` | Comma-separated list of security identifiers to supervise. When empty the strategy falls back to the assigned `Security`. | `"GBPUSD,USDCAD,USDCHF,USDSEK"` |
| `ProfitTarget` | Net profit (in portfolio currency) required to trigger a global close of all watched positions. | `60` |
| `MaxLoss` | Maximum acceptable loss (in portfolio currency) before the strategy force-closes the basket. | `60` |
| `Slippage` | Compatibility parameter that reflects the allowed slippage from the original script. Market orders are used for exits, so the value is informational. | `10` |
| `MinAgeSeconds` | Minimum lifetime of a position before the strategy is allowed to close it. | `60` |
| `CandleType` | Candle type used for periodic supervision (default: 1-minute candles). | `1 minute` |

## Notes

- The strategy relies on `Positions[i].PnL` provided by StockSharp to measure floating profit. It does not pull trade history or compute prices manually.
- Positions opened before the strategy starts inherit the start time as their first seen timestamp. They will be closed only after the `MinAgeSeconds` interval elapses from strategy start.
- Exits are executed with market orders to maximise the probability of immediate liquidation. `Slippage` is logged for parity with the MQL version but is not applied to price calculations.
- Logging output replicates the MetaTrader "Comment" window by printing each symbol's profit followed by the overall basket total.

## Requirements

- Assign a valid `SecurityProvider` or ensure the requested identifiers are available through the connector.
- Provide sufficient volume configuration per security so that market orders can flatten the position completely.

