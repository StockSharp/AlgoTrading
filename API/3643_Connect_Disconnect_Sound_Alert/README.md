# Connect Disconnect Sound Alert Strategy

## Overview
The **Connect Disconnect Sound Alert Strategy** continuously monitors the connection status of the strategy connector and logs every transition between online and offline states. The original MQL5 expert played audio files when the MetaTrader terminal was connected or disconnected. This C# conversion keeps the core logic – detecting connection changes – and exposes hooks that allow the StockSharp runtime to record events and durations. The strategy can be used as a lightweight watchdog that informs the operator about connectivity problems without placing any orders.

## Key Features
- Periodically polls the connector state using a configurable interval.
- Detects both connection and disconnection events and writes detailed log entries.
- Records how long the terminal stayed online or offline (optional).
- Skips notification sounds on the very first check to mirror the MQL behavior.

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `CheckIntervalSeconds` | `1` | Number of seconds between connector status checks. Must be greater than zero. |
| `LogDurations` | `true` | When enabled, the strategy logs the time span that the connection stayed online or offline after each transition. |

All parameters are exposed through `StrategyParam<T>` so they can be modified from the UI or during optimization.

## How It Works
1. When the strategy starts it stores the current connector state and, optionally, logs the initial status.
2. A `System.Threading.Timer` periodically calls an internal handler that compares the current connection flag with the previous value.
3. If the state changed, the strategy logs the transition. The very first notification is marked as "initial" and does not represent an actual sound alert (matching the original expert advisor logic).
4. Optional duration logs show how long the previous state lasted, helping the operator evaluate connection stability.
5. The timer is automatically disposed when the strategy stops or resets.

## Usage Notes
- Attach the strategy to any connector-enabled StockSharp terminal. It does not interact with market data or place orders.
- Keep the default polling interval for near real-time monitoring. Increase the value if you only need coarse updates.
- The strategy uses the StockSharp logging subsystem (`LogInfo`). Configure log listeners or dashboards to see the notifications.
- To add actual sound alerts, connect a notification service in your host application and play audio when log messages arrive.

## Safety Considerations
- The strategy validates the polling interval and throws an exception if it is not positive.
- Timer callbacks use the strategy `CurrentTime` to ensure consistent timestamps even when historical data replay is used.
- All resources are released on stop/reset to avoid background timers after the strategy is disabled.
