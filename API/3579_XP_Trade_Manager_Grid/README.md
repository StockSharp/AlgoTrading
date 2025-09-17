# XP Trade Manager Grid (StockSharp Port)

## Strategy overview
The original **XP Trade Manager Grid** expert advisor is a position manager that averages into
the market whenever the price moves against the latest entry by a fixed distance. Each newly
added order is tagged with `order1`, `order2`, …, `order15` and has its own profit target. The
first three grid levels use fixed take profit distances, while levels four to fifteen rely on a
break-even calculation plus an incremental profit target. Once a collective target is reached the
entire basket is closed. The EA also keeps track of the cumulative profit delivered by the first
order and automatically re-opens it if the most recent take profit left the sequence below the
configured total target.

This StockSharp implementation reproduces the same grid management rules using the repository
high level API (`SubscribeCandles().Bind(...)`). Market data candles are only used as timing
events: every completed candle triggers the management logic, which reads the latest best bid/ask
prices to evaluate distance and profit conditions.

## Execution flow
1. **Data subscription** – the strategy subscribes to the configured candle type. No indicators are
   required, because the logic operates purely on price differences.
2. **Stage take profit control** – on every finished candle the manager verifies whether the first
   three grid levels reached their individual take profit distances (`TakeProfit1Partitive`,
   `TakeProfit2`, `TakeProfit3`). When a target is hit an opposite market order with the
   `orderN_tp` comment is submitted to close only that level.
3. **Grid expansion** – if the last opened order of a direction moved against the current price by
   at least `AddNewTradeAfter` points, the next stage (`order{N+1}`) is added, respecting the
   `MaxOrders` cap and keeping track of pending registrations to avoid duplicates.
4. **Basket take profit** – when at least four positions are active the strategy computes the
   weighted-average break-even price and adds the configured total profit (per active count). If
   the current price crosses the derived target the complete basket is closed with the
   `breakeven_exit` comment.
5. **Risk control** – floating profit and loss is recalculated each candle. If the drawdown exceeds
   `RiskPercent` percent of the portfolio value the whole basket is liquidated with the
   `risk_exit` tag.
6. **First-order renewal** – whenever an `order1` position finishes (either through its dedicated
   take profit or a global exit) the realized pips and currency gain are stored together with the
   direction and take profit price. If the cumulative profit stays below `TakeProfit1Total` and the
   market moves away from the last take profit by `TakeProfit1Offset` points, a new `order1` is
   automatically created in the same direction.

## Parameter mapping
| MetaTrader input            | StockSharp parameter          | Description |
|-----------------------------|-------------------------------|-------------|
| `AddNewTradeAfter`          | `AddNewTradeAfter`            | Grid spacing in points between consecutive entries. |
| `TakeProfit1Partitive`      | `TakeProfit1Partitive`        | Partial take profit distance for `order1`. |
| `TakeProfit2`               | `TakeProfit2`                 | Take profit distance for `order2`. |
| `TakeProfit3`               | `TakeProfit3`                 | Take profit distance for `order3`. |
| `TakeProfit4Total` … `15`   | `TakeProfitXTotal`            | Total profit (points) added on top of break-even when N orders are open. |
| `TakeProfit1Total`          | `TakeProfit1Total`            | Total profit target accumulated by the first order before auto-renewing. |
| `TakeProfit1Offset`         | `TakeProfit1Offset`           | Minimum distance from the last TP before a fresh `order1` can be opened. |
| `MaxOrders`                 | `MaxOrders`                   | Upper bound of simultaneous grid entries per direction. |
| `Risk`                      | `RiskPercent`                 | Maximum floating loss allowed, measured as percent of the portfolio value. |
| `Lots`                      | `OrderVolume`                 | Volume submitted for each averaging order. |
| —                           | `CandleType`                  | Candle subscription used to trigger the management loop. |
| —                           | `AutoRenewFirstOrder`         | Enables the automatic re-entry logic for the first order. |

All price-based distances are converted from “points” using `Security.PriceStep`. The algorithm
also works with synthetic or crypto instruments thanks to fallbacks for both `PriceStep` and
`StepPrice`.

## Implementation notes
- High level order helpers (`BuyMarket` / `SellMarket`) are used everywhere. Comments on the
  generated orders match the MQL names, which makes the trade blotter easy to interpret.
- Pending stage tracking ensures that the strategy never sends duplicate `orderN` requests while a
  previous attempt is still live.
- The floating PnL estimation leverages the instrument step value, which allows the risk watchdog to
  act even if the broker does not provide unrealized PnL in real time.
- The automatic renewal logic keeps the same behaviour as the EA: the first order is only re-opened
  if the accumulated profit is still below `TakeProfit1Total` and the price has moved away from the
  last take profit by at least `TakeProfit1Offset` points.

## Differences vs. the original EA
- The MetaTrader labels drawn on the chart are not recreated. All relevant statistics (accumulated
  pips and currency for the first order) are kept internally and can be logged or exposed via
  custom UI components if desired.
- The StockSharp version uses candle events instead of tick events. Using a 1-minute candle stream
  reproduces the tick-by-tick behaviour closely while keeping compatibility with the repository
  rules.
- Take profit orders are executed via market exits instead of modifying position-level TP/SL values.
  This keeps the implementation broker-agnostic and matches the high level API philosophy.

## Usage
1. Attach the strategy to a portfolio and symbol, configure the grid spacing, take profit ladder and
   risk parameters.
2. Leave `AutoRenewFirstOrder` enabled if the EA should keep restarting the grid automatically after
   successful `order1` take profits. Disable it to manage the first entry manually.
3. Start the strategy. All trade management (scale-ins, exits, risk liquidations) is handled
   automatically; manual intervention is only required if a custom initial bias is desired.

## Safety considerations
Grid strategies can accumulate significant exposure. Always double-check `MaxOrders`,
`OrderVolume`, and `RiskPercent` before running on a live portfolio. Consider using the built-in
backtester to validate the behaviour under different volatility regimes.
