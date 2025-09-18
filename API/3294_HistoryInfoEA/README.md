# HistoryInfoEaStrategy

## Overview
The **HistoryInfoEaStrategy** replicates the MT4 "HistoryInfo" utility on top of StockSharp. Instead of drawing text on the MetaTrader chart, the strategy listens to the `OnNewMyTrade` stream and aggregates statistics for trades that match a chosen filter. The aggregated values are exposed through the `LastSnapshot` property and mirrored in the strategy log so that a GUI or automation script can display the summary in any preferred form.

The strategy never registers its own orders. It is designed to run alongside other automated or manual strategies while they submit orders to the broker. Every filled trade that satisfies the filter contributes to the totals.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `FilterType` | Selection mode that determines how trades are matched. Supported values: `CountByUserOrderId`, `CountByComment`, `CountBySecurity`. |
| `MagicNumber` | Expected `Order.UserOrderId`. Used only when `FilterType` equals `CountByUserOrderId`. Leave empty to disable this filter. |
| `OrderComment` | Prefix that must match `Order.Comment`. Only relevant for the `CountByComment` mode. The default value (`"OrdersComment"`) mimics the MT4 script placeholder and usually does not match any order until replaced. |
| `SecurityId` | Identifier of the instrument (`Security.Id`) that must match when `FilterType` equals `CountBySecurity`. The default (`"OrdersSymbol"`) is a placeholder. |

## Aggregated metrics
`LastSnapshot` is updated after every matching trade. It contains:

- `FirstTrade` / `LastTrade` – timestamps of the earliest and latest processed trades.
- `TotalVolume` – cumulative filled volume expressed in the trade's volume units (lots, contracts, etc.).
- `TotalProfit` – sum of `MyTrade.PnL` minus reported commission, giving the realised profit in account currency.
- `TotalPips` – profit converted into pips using `Security.PriceStep`, `Security.StepPrice` and MT4-like digit handling (5/3 digits multiply the point by 10).
- `TradeCount` – number of trades that passed the filter.

The same information is printed to the strategy log in a single line, emulating the MT4 `Comment()` output for quick inspection.

## Usage
1. Attach the strategy to the same portfolio and security that other strategies use for order submission.
2. Pick the desired `FilterType` and fill the associated parameter (magic number, comment prefix, or security identifier).
3. Start the strategy. As soon as the first trade that matches the criteria is filled, the totals become available through `LastSnapshot` and the log.
4. The counters reset automatically on every strategy restart or manual reset.

> **Note:** To compute pip totals the strategy relies on correct instrument metadata. Ensure that `Security.PriceStep` and `Security.StepPrice` are configured in the board definition. If either value is missing the pip counter stays at zero while the profit value continues to accumulate.

## Conversion notes
- The MT4 code iterated over `OrdersHistoryTotal()` on every tick. In StockSharp the strategy reacts to real-time `MyTrade` notifications, so there is no polling and the calculations update immediately when a fill arrives.
- MT4 stored profit as `OrderProfit + OrderCommission + OrderSwap`. StockSharp delivers realised profit via `MyTrade.PnL` and commission separately; swap is usually already included in the PnL. The port subtracts commission from `PnL` to keep consistency with the original report.
- The string placeholders (`"OrdersComment"`, `"OrdersSymbol"`) are preserved to resemble the original defaults. Replace them with actual values before starting the strategy if you expect matches.
- Visual chart output from MT4 is replaced by structured data (`LastSnapshot`) and log lines so that integrators can decide how to render the information.
- The strategy purposefully avoids creating any new orders, so it can be launched in read-only mode to analyse third-party trade streams without interfering with them.

## Extensibility ideas
- Subscribe to the `LastSnapshot` updates and forward the information to a dashboard or telemetry collector.
- Extend the class with additional filters (for example by portfolio or custom strategy tags) if the connector provides the relevant metadata.
- Combine the strategy with a periodic timer to export historical summaries to a CSV/JSON report.
