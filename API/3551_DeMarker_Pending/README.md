# DeMarker Pending Strategy

## Overview
This StockSharp strategy reproduces the behaviour of the MetaTrader expert advisor "DeMarker Pending 2.5". The bot evaluates the DeMarker oscillator on a configurable timeframe and, when extreme levels are crossed, it places a pending order in the breakout direction. The order can be either a stop or a limit order offset by a fixed number of points. Optional trade window filtering and automatic expiration keep pending orders aligned with the original expert's behaviour.

## Trading logic
- Subscribe to the selected candle series and calculate the DeMarker indicator with period `DemarkerPeriod`.
- Detect crossovers of the lower (`DemarkerLowerLevel`) and upper (`DemarkerUpperLevel`) thresholds using the current and previous finished candle values.
- When the lower level is crossed upward, queue a long setup; when the upper level is crossed downward, queue a short setup.
- Convert setups into pending orders at price `Close ± PendingIndentPoints * PriceStep`, using stop orders in breakout mode or limit orders for pullback entries depending on `Mode`.
- Attach stop-loss and take-profit levels to the pending order by offsetting the entry price by `StopLossPoints` and `TakeProfitPoints` points.
- Cancel or reuse older pending orders according to `ReplacePreviousPending` and `SinglePendingOnly` before registering a new one.
- Remove pending orders automatically once their `PendingExpirationMinutes` lifetime elapses.
- Ignore signals outside of the intraday window when `UseTimeWindow` is enabled. Every bar is processed only once, so at most one new pending order is created per bar and direction.

## Order management
- All entries are created as pending orders (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`).
- Each pending order carries its own stop-loss and take-profit prices so that the position is protected immediately after activation.
- Pending orders are cancelled on expiration, when replaced by new setups, or when the order state changes to an inactive status (filled, cancelled, rejected).

## Parameters
| Parameter | Description |
|-----------|-------------|
| `Volume` | Order volume in lots. |
| `StopLossPoints` | Distance between entry price and stop-loss in points. |
| `TakeProfitPoints` | Distance between entry price and take-profit in points. |
| `PendingIndentPoints` | Offset between market price and the pending order. |
| `PendingExpirationMinutes` | Lifetime of each pending order in minutes (0 disables expiration). |
| `Mode` | Pending order type (stop for breakouts or limit for pullbacks). |
| `SinglePendingOnly` | If enabled, prevents placing more than one active pending order. |
| `ReplacePreviousPending` | Cancels active pending orders before issuing a new one. |
| `DemarkerPeriod` | Lookback period of the DeMarker oscillator. |
| `DemarkerUpperLevel` | DeMarker threshold that triggers sell setups. |
| `DemarkerLowerLevel` | DeMarker threshold that triggers buy setups. |
| `CandleType` | Timeframe used for candle subscription and indicator evaluation. |
| `UseTimeWindow` | Enables intraday time filtering. |
| `StartTime` | Start of the intraday trading window. |
| `EndTime` | End of the intraday trading window. |

## Notes
- The original expert includes sophisticated money management and trailing-stop routines. This port keeps the signal generation and pending order handling but simplifies position sizing to a single fixed `Volume` parameter.
- StockSharp attaches stop-loss and take-profit prices at order registration time; depending on the broker, you may need to verify that stop and limit orders support those protective levels.
- Always ensure the point-based distances are compatible with the traded symbol's `PriceStep`. Set `PendingIndentPoints`, `StopLossPoints`, and `TakeProfitPoints` to values that satisfy the broker's minimum distance requirements.
