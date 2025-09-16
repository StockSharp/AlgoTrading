# Order Time Alert Strategy

## Overview

The **Order Time Alert Strategy** monitors all active orders and generates an alert when any order remains active for more than a specified number of seconds. The strategy is useful for notifying the trader about orders that have been waiting too long without execution.

## Parameters

- `AlertDelaySeconds` – number of seconds an order must remain active before an alert is triggered.
- `TimerFrequencySeconds` – how often the strategy checks active orders.
- `UseLogging` – if enabled, the strategy writes a message to the log when an alert occurs.
- `SoundName` – name of the sound file for the alert. The sound is not played in this implementation, but the value is preserved for compatibility.

## How It Works

1. When the strategy starts, it sets up a periodic timer based on `TimerFrequencySeconds`.
2. At each timer tick, the strategy scans all active orders.
3. If an order has been active longer than `AlertDelaySeconds` and has not been alerted yet, the strategy logs a warning message.
4. Each order is alerted only once.

## Notes

- The strategy uses the high-level StockSharp API and is designed for monitoring purposes, not for automated trading.
- To play a sound notification, integrate the desired audio mechanism using the `SoundName` parameter.

