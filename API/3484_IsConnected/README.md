# IsConnected Strategy

## Summary
* **Source**: Converted from the MetaTrader 5 script `IsConnected.mq5` (folder `MQL/35056`).
* **Purpose**: Continuously monitors the connector status and reports online/offline transitions with timestamps and uptime/downtime durations.
* **Type**: Utility strategy focused on infrastructure monitoring rather than order execution.

## Behaviour
1. When the strategy starts it immediately logs that the monitoring module has been initialised and captures the current connector state.
2. A background timer checks the `Connector.IsConnected` flag every `CheckIntervalSeconds` (default: 1 second).
3. When the state changes, the strategy:
   * Stores the moment of transition using the strategy `CurrentTime`.
   * Logs the new state (`Online` or `Offline`).
   * Reports how long the previous state lasted (time online before a disconnect, or time offline before reconnection).
4. When the strategy stops, it cancels the timer and logs the last known state so the operator knows whether the connection was up or down at shutdown.

## Parameters
| Name | Type | Default | Description |
|------|------|---------|-------------|
| `CheckIntervalSeconds` | `int` | `1` | Interval (in seconds) between successive connection checks. Must be greater than zero. |

## Logging details
* All messages are written with `LogInfo` in English to match the MetaTrader implementation that relied on `Print` statements.
* Time intervals are formatted using invariant culture and include both start timestamps and the time spent in the previous state.

## Differences vs original script
* The busy waiting loop from MQL5 is replaced with a managed timer that does not block the strategy thread.
* Instead of printing duplicate status lines, the StockSharp version reports structured status changes along with uptime/downtime metrics.
* The conversion handles graceful disposal by stopping the timer in both `OnStopped` and `OnReseted`.
