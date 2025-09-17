# ShowHistoryOnChart Strategy

## Overview
The **ShowHistoryOnChartStrategy** replays previously executed trades that were exported from MetaTrader into a CSV file. It opens and closes positions in the StockSharp environment according to the historical schedule. The strategy is intended for visual inspection of past deals on live charts or during market replays without calculating any indicators.

## Original MQL Idea
The MetaTrader expert advisor "ShowHistoryOnChart-V1.1" reads trade history from a CSV file and draws arrows and trend lines on the chart to highlight past deals. It does not execute any trades by itself. The StockSharp port replicates the idea by executing market orders at the recorded timestamps so that the built-in chart visualization (Own Trades layer) displays the same entries and exits.

## Trading Logic
1. **CSV Parsing** – During startup the strategy loads the CSV file specified in the `FileName` parameter. The file must be semicolon-separated and contain the following columns per trade: open time, trade type (Buy/Sell), volume, symbol, open price, additional volume column, close time, close price, commission, swap, and profit.
2. **Symbol Filtering** – Only trades whose `symbol` column matches the selected security (by `Security.Id` or `Security.Code`) are scheduled.
3. **Scheduling** – Trades are sorted by their open time. Any trades that start before the current strategy time are skipped to avoid replaying completed history.
4. **Replay Loop** – The strategy subscribes to the candle type selected in `CandleType`. When a candle closes:
   - All pending trades with an open time less than or equal to the candle close time are opened using market orders (`BuyMarket` for Buy trades, `SellMarket` for Sell trades).
   - Active trades whose close time is less than or equal to the candle close time are closed with the opposite market order.
5. **Visualization** – Because actual orders are executed, the standard `DrawOwnTrades` helper displays the entries and exits on the chart, mimicking the original indicator arrows.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `FileName` | Absolute or relative path to the CSV trade history file. | Empty string |
| `CandleType` | Candle type that drives the scheduling timeline (for example, 1-minute time frame). | 1-minute time frame |

## Additional Notes
- The CSV parser expects timestamps that can be parsed by `DateTimeOffset.Parse` or `DateTime.Parse` in invariant culture (e.g., `2023.05.01 12:30`).
- Volumes are parsed from the third column (`volume1` in the original MQL script) and automatically converted to absolute values.
- Trades with invalid data (missing fields, wrong order type, close time before open time) are skipped with a warning in the log.
- The strategy does not calculate indicators and works purely as a scheduler for market orders.
