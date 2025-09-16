# Martin For Small Deposits
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
This strategy reproduces the "Martin for small deposits" averaging expert in StockSharp. It looks at 15 completed candles and opens a position only when the newest close is below (for longs) or above (for shorts) the close recorded 14 bars earlier. All trades are executed at market using the high-level strategy API, and the logic is applied once per finished candle.

## Entry Logic
- A rolling buffer keeps the last 15 completed candle closes.
- When there are no open or pending positions, the strategy compares the most recent close with the close 14 bars ago.
- If the latest close is lower, a long grid is started; if it is higher, a short grid is started.
- Trade volume for the first order equals **Initial Volume**. Subsequent entries on the same side use the martingale multiplier before being normalized to the instrument's volume step.

## Position Management
- While a position exists, the strategy waits for **Bars To Skip** finished candles before considering another averaging trade.
- Additional orders are sent only if price moves against the current direction by at least **Step (pips)**, converted to price units using the detected pip size.
- Each execution updates internal statistics: aggregated volume, average entry price, lowest (for longs) or highest (for shorts) entry price, and the price of the most recent fill.
- Volume never exceeds **Max Volume** or the exchange-defined maximum volume. If the normalized size falls below the minimum allowed volume, the order is skipped.

## Exit Conditions
- When the unrealized net profit (difference between the current close and the average entry price, multiplied by position volume) exceeds **Min Profit**, all open orders are flattened.
- If **Take Profit (pips)** is greater than zero and price reaches that distance from the latest entry in the favorable direction, the entire grid is closed.
- Closing requests are tracked; no new orders are sent until exit orders are fully filled. After a flat state is reached, all internal counters reset so the next signal starts a fresh grid.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| Initial Volume | 0.01 | Base lot size for the first trade. |
| Take Profit (pips) | 65 | Distance in pips from the latest fill that triggers a full exit. Use 0 to disable this check. |
| Step (pips) | 15 | Adverse movement in pips required before averaging into the position. |
| Bars To Skip | 45 | Minimum number of finished candles to wait between averaging orders. |
| Increase Factor | 1.7 | Multiplier applied to the trade volume each time a new order is added on the same side. |
| Max Volume | 6 | Upper bound for aggregated volume (before normalization by market limits). |
| Min Profit | 10 | Profit target used to close the entire grid when the net profit exceeds this amount. |
| Candle Type | 1 hour | Timeframe used for candle subscription and signal calculations. |

## Implementation Notes
- Pip size is derived from `Security.PriceStep` and decimal precision. For instruments quoted with 3 or 5 decimals, the code multiplies the price step by 10 to match the MQL concept of a pip.
- Unrealized profit is approximated from price differences and does not include swap or commission adjustments that were present in the original expert.
- Additional averaging trades are skipped while exit orders are active, preserving the sequential execution flow of the original MQL logic.
- When **Step (pips)** is zero the strategy never averages; when **Take Profit (pips)** is zero only the **Min Profit** condition closes the grid.
