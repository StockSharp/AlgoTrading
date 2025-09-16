# List Positions Strategy

## Overview
The **List Positions Strategy** reproduces the behaviour of the original MetaTrader script by periodically printing the current portfolio positions to the strategy log. It is a monitoring-only helper that never places orders. Instead, it builds a snapshot of the open positions so that the operator can inspect symbol, direction, size, entry price and current profit directly from the Designer or StockSharp logs.

## Key Features
- Timer-driven position reporting with the first snapshot delivered immediately after the strategy starts.
- Optional filtering by the strategy security or by strategy identifier (the analogue of the MetaTrader magic number).
- Detailed log output including the position identifier, last change time, side, quantity, average price and profit.
- Thread-safe processing that avoids overlapping timer callbacks when the environment is busy.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `StrategyIdFilter` | Strategy identifier to skip. When left empty all positions are reported. | Empty string |
| `SelectionMode` | Controls whether positions from every symbol or only from `Strategy.Security` are reported. | `AllSymbols` |
| `TimerInterval` | Interval between consecutive position snapshots. | 6 seconds |

## How It Works
1. During `OnStarted` the strategy verifies that a portfolio is attached and that the timer interval is positive.
2. A `System.Threading.Timer` is created with zero delay so the first report is produced immediately and then repeated at the configured interval.
3. Each timer tick calls `ProcessPositions`, which iterates over `Portfolio.Positions`, applies the optional symbol and strategy-id filters, and appends formatted lines to a `StringBuilder`.
4. When at least one position passes the filters the assembled table is written to the log with `LogInfo`. If nothing matches, a concise notification is logged instead.
5. Timer overlaps are prevented with an interlocked guard so that slow I/O cannot trigger concurrent executions.

## Usage Notes
- Assign both `Portfolio` and `Connector` before starting the strategy. If `SelectionMode` is set to `CurrentSymbol`, also set `Strategy.Security` to the instrument you wish to monitor.
- To emulate the MetaTrader `magic` filter, fill `StrategyIdFilter` with the string value used as `StrategyId` when other strategies submit orders. Those positions will be excluded from the report.
- The strategy never modifies positions or registers orders, making it safe to run alongside live trading logic as an informational widget.
- Log output is grouped under the column header `Idx | Symbol | PositionId | LastChange | Side | Quantity | AvgPrice | PnL` so it can be easily parsed by external tooling if necessary.

## Differences Compared to the MQL Version
- MetaTrader uses an unsigned 64-bit `magic` number. StockSharp positions expose the strategy identifier as a string, therefore the filter accepts textual values.
- Instead of writing to the chart comment, this port records the snapshot via `LogInfo`, which is visible in Designer, Runner or any log listener.
- The StockSharp version guards against overlapping timer invocations to stay responsive under heavy load.
- Time stamps rely on `Position.LastChangeTime`, which reflects StockSharp position updates, while the MQL script displayed the ticket creation time.
