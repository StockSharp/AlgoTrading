# Reverse Strategy

## Overview
The **Reverse Strategy** is a StockSharp port of the classic MetaTrader 4 script `Reverse.mq4` by KimIV. The original tool was a
one-click utility: it closed every open trade and immediately opened a new position in the opposite direction while preserving
lot size and optional stop-loss/take-profit levels. The StockSharp version performs the same portfolio flip for the connected
account, allowing discretionary traders or automated workflows to neutralise and reverse exposure without manually staging
orders.

## How it works
1. When the strategy starts it collects the positions that need to be processed. By default it works only on the strategy
   security, but it can also sweep the entire portfolio when `CurrentSymbolOnly = false`.
2. For every long position the strategy sends two market sell orders: the first one closes the existing exposure, the second one
   opens the new short of the same size. Short positions are flipped by sending two market buy orders in the same fashion.
3. Unless `MarketWatchMode` is enabled, the strategy immediately places protective orders around the new position. Stop-losses
   and take-profits are expressed in pips and converted to absolute prices using the security tick size (`PriceStep` or
   `MinStep`). Stops are implemented with `SellStop`/`BuyStop` orders and targets with `SellLimit`/`BuyLimit` orders.
4. Order submission is wrapped in a configurable retry loop to mimic the defensive checks present in the MQL script. Any
   exception is logged and retried up to `RetryCount` times before the strategy fails fast.
5. After all selected securities have been processed, the strategy stops itself. Protective orders remain working in the system
   exactly like their MetaTrader counterparts.

## Differences compared to the MQL script
- StockSharp aggregates all trades of the same security into a single position. The reversal therefore uses the net position
  size instead of iterating over individual tickets. The final exposure matches the behaviour of flipping each ticket one by one.
- Stop-loss and take-profit are registered as separate protective orders because StockSharp market orders cannot embed SL/TP
  prices at submission time. The resulting execution logic is equivalent to the post-modification performed in the original
  `OpenPosition` function.
- `SlippagePoints` is kept for compatibility with the script input but is only stored for analytics. Market orders are executed
  at the best available price provided by the connector.
- `MarketWatchMode` reproduces the broker safeguard that prevented attaching stops directly to market orders. When enabled, no
  protective orders are placed automatically.
- `RetryCount` replaces the `NumberOfTry` loops from the script. Each attempt simply calls the high-level API again, letting the
  connector decide whether the order can be accepted.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `StopLossPips` | `30` | Distance (in pips) between the entry price and the protective stop. Set to `0` to disable stops. |
| `TakeProfitPips` | `50` | Distance (in pips) between the entry price and the profit target. Set to `0` to disable the target. |
| `CurrentSymbolOnly` | `true` | When `true`, only the position of `Strategy.Security` is flipped. Otherwise all portfolio positions are processed. |
| `MarketWatchMode` | `false` | Skip attaching protective orders. Useful for brokers that require manual stop/take placement after execution. |
| `SlippagePoints` | `3` | Reserved parameter that mirrors the MT4 input. Currently stored for reference in logs and UI. |
| `RetryCount` | `3` | Maximum number of attempts for each market order if the connector raises an exception. |

## Usage notes
- Ensure the strategy portfolio and security are linked to the same connector that holds the positions you intend to flip.
- Run the strategy only when there are active positions; otherwise it will perform no action and immediately stop.
- For symbols without a configured `PriceStep`/`MinStep` the protective offsets cannot be calculated, so stops and targets are
  skipped even if the pip parameters are positive.
- Protective orders are placed with the full reversed volume. Cancel or adjust them manually if you later modify the position.

## Related files
- Original source: [`MQL/8756/Reverse.mq4`](../../MQL/8756/Reverse.mq4)
- C# strategy: [`CS/ReverseStrategy.cs`](CS/ReverseStrategy.cs)
