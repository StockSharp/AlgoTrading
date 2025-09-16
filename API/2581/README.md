# Close Positions By Time Strategy

## Overview
This strategy is a conversion of the MetaTrader 5 expert advisor `Exp_ClosePositionsByTime.mq5`. The original robot monitors the trading server time and, once a user defined cut-off point is reached, it flattens every position that belongs to the current symbol. The StockSharp port keeps the same behaviour by watching finished candles and forcing a full exit as soon as the candle close time moves beyond the configured deadline.

Unlike the MQL version, which reacts on every tick, the StockSharp implementation uses StockSharp's high-level candle subscription API. This approach keeps the code concise and lets the platform handle the data flow and order routing details.

## Trading Rules
1. Subscribe to the selected candle series (default: 1 minute) in order to track market time reliably.
2. For every finished candle, compare the candle close timestamp with the `StopTime` parameter.
3. Before the stop time is reached, the strategy remains idle and does not interfere with any open trades.
4. After the stop time is exceeded:
   - Cancel every working order belonging to the strategy to prevent re-entry.
   - If the aggregated position is non-zero, call `ClosePosition()` so StockSharp sends the correct market order (sell for a long position, buy for a short position).
5. Continue to monitor subsequent candles. Any new exposure opened after the stop time will be flattened again on the next finished candle, mimicking the loop that existed in the MQL expert.

## Parameters
| Name | Type | Description |
| ---- | ---- | ----------- |
| `StopTime` | `DateTimeOffset` | Absolute date and time after which all positions must be liquidated. The default mirrors the MQL value `2030-01-01 23:59`. |
| `CandleType` | `DataType` | Candle series used to advance time. Keep the interval small (for example 1 minute) to ensure the exit happens as soon as possible after the deadline. |

## Implementation Notes
- The strategy lives in `CS/ClosePositionsByTimeStrategy.cs` and derives from `Strategy`.
- Tabs are used for indentation, matching the repository guidelines.
- All inline comments are written in English, as required by the global instructions.
- `StartProtection()` is enabled on start to let StockSharp supervise unexpected fills after the forced exit.
- No Python port is provided, per the task request.

## Original Expert Advisor Behaviour
The MQL expert performed the following operations:
- User inputs `StopTime` and `Deviation_` (allowed slippage).
- Every tick it checked `TimeCurrent()`. Once the current time was greater than `StopTime`, it iterated through all positions, selected those belonging to the chart symbol, and closed them by sending market orders in the opposite direction.
- After all positions were closed the `Stop` flag was reset so that any future position would also be liquidated immediately.

The StockSharp version translates these mechanics to StockSharp conventions. The `Deviation_` setting has no direct equivalent in StockSharp's market order helpers, therefore the conversion relies on `ClosePosition()` which already issues market orders at the best available price.

## Usage Tips
- Combine this strategy with any entry logic by running it as an auxiliary "session guardian" that closes positions at a strict time of day.
- When backtesting, ensure the data stream covers the desired cut-off time; otherwise the strategy will not receive a finished candle and cannot trigger the exit.
- To implement multiple cut-off windows, duplicate the strategy or extend it with an additional schedule array.
