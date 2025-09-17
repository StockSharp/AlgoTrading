# Get Last Nth Close Trade Strategy

## Overview
The **Get Last Nth Close Trade Strategy** reproduces the behaviour of the MetaTrader 4 expert advisor `Get_Last_Nth_Close_Trade.mq4`. The original script continually inspects the terminal trade history, sorts the closed deals by their closing time, and prints the details of the *n*-th trade counted from the most recent entry. This C# port uses the StockSharp high level strategy API to accomplish the same workflow in a managed environment.

The strategy observes all orders issued by the strategy instance, keeps a record of the ones that reach the `Done` state, sorts the records in reverse chronological order, and logs the fields of the trade located at the user defined index. When no closed trades satisfy the selected filters, the strategy emits informative log entries explaining the current state.

## Behaviour mapping
| MetaTrader 4 concept | StockSharp implementation |
| --- | --- |
| `OrdersHistoryTotal` and manual loops | The strategy subscribes to order change notifications and maintains an internal list of closed orders. |
| Optional symbol filter (`ENABLE_SYMBOL_FILTER`) | When enabled, the strategy only records orders whose `Security` matches `Strategy.Security`. |
| Optional magic number filter (`ENABLE_MAGIC_NUMBER`) | When enabled, the strategy compares the numeric value stored in the order comment with the configured magic number. |
| Sorting by `OrderCloseTime` | Closed orders are stored with their last change time and sorted in descending order. |
| `Comment()` call that prints a multi-line summary | `AddInfo` is used to push the formatted summary to the strategy log. |

## Parameters
- **Enable Magic Number** – toggles the filter that matches closed orders by a numeric comment interpreted as the magic number.
- **Enable Symbol Filter** – limits the inspection to orders that use the strategy security.
- **Magic Number** – numeric identifier compared with the order comment when the magic filter is active.
- **Trade Index** – zero-based index of the trade to display, starting from the most recent closed deal.

All parameters can be optimised through the standard StockSharp optimisation facilities because they are exposed via `StrategyParam<T>`.

## Output
Whenever the list of closed trades changes, or when it is empty, the strategy prints a multi-line message similar to the original EA:

```
ticket 123456
symbol EURUSD@FXCM
lots 1
openPrice 1.2345
closePrice 1.2350
stopLoss 0
takeProfit 0
comment 1234
type Market
orderOpenTime 2024-03-01T10:15:00+00:00
orderCloseTime 2024-03-01T11:00:00+00:00
profit 0.0005
```

The text is refreshed only when the value changes to avoid flooding the log window.

## Limitations
- StockSharp does not expose MetaTrader style magic numbers, therefore the filter expects the numeric identifier to be placed inside the order comment by the component that submits orders.
- Profit, stop loss, and take profit values are derived from the information that StockSharp provides for the finished order. If the brokerage adapter does not supply those fields, the reported numbers remain zero.
- The port only monitors orders created by the strategy instance. Historical orders that were executed before the strategy started cannot be retrieved retrospectively.

## Usage notes
1. Attach the strategy to a security and portfolio, then start it normally.
2. If you need to mimic the MetaTrader magic number filtering, ensure that the order comment contains an integer that matches the **Magic Number** parameter.
3. Adjust **Trade Index** to select the trade counted from the most recent closed order (0 = most recent, 1 = second most recent, etc.).
4. Monitor the strategy log for the formatted summary.

## Files
- `CS/GetLastNthCloseTradeStrategy.cs` – C# implementation of the strategy.
- `README.md` – this documentation.
- `README_cn.md` – Simplified Chinese translation.
- `README_ru.md` – Russian translation.

