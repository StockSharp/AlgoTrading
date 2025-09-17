# VR Watch List Linker Lite

## Overview
VR Watch List Linker Lite reproduces the MetaTrader utility that synchronized every open chart with the active symbol and timeframe. The StockSharp port keeps a configurable set of securities aligned with the master chart selected for the strategy. All linked instruments subscribe to the same candle series, share the same timeframe, and are automatically restarted when they drift away from the master stream. The component is useful when several dashboards must always follow the same instrument without manually updating each window.

The strategy is non-trading by design. It focuses exclusively on market data coordination: subscriptions are started and monitored, but no orders are placed.

## Synchronization logic
### Linking workflow
* When the strategy starts it builds the list of securities to manage. The primary security is optional, but when enabled it becomes the master chart whose candles act as the reference clock.
* Every linked security (including the master when selected) subscribes to the configured candle `DataType`. The strategy logs each successful link for audit purposes.
* Incoming candles update the latest open time recorded for each security. The master open time becomes the expected timestamp for the rest of the list.

### Drift detection and recovery
* Whenever a linked security produces a finished candle, its timestamp is compared with the master open time.
* If the timestamps differ, the strategy either restarts the subscription immediately or simply reports the mismatch, depending on the `Restart On Mismatch` parameter.
* Restarting disposes the current subscription and creates a fresh one with the same settings, forcing the feed to resynchronize with the master stream.

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `Candle Type` | Candle `DataType` shared by the master chart and every linked security. Only `TimeFrameCandleMessage` based types are supported. | M1 timeframe |
| `Linked Securities` | Additional securities that should follow the master chart. Leave empty to control only the primary security. | empty |
| `Include Primary` | When enabled the strategy security becomes the master chart. Disable it to synchronize only the external list. | `true` |
| `Restart On Mismatch` | Automatically restart the subscription of a linked security when its candles fall out of sync with the master timestamp. | `true` |

## Usage
1. Assign the desired instrument to the strategy security (this becomes the master chart when **Include Primary** stays enabled).
2. Populate **Linked Securities** with any extra instruments that should mirror the master chart. Duplicates and null entries are ignored.
3. Choose the shared **Candle Type** timeframe. Custom timeframes are supported as long as they rely on `TimeFrameCandleMessage`.
4. Start the strategy. The log will confirm every linked security and report future re-subscriptions or mismatches.

Stopping or resetting the strategy disposes every subscription to avoid stale data channels. Restarting the strategy re-creates them from scratch.

## Additional notes
* The strategy never sends orders; it is intended for data alignment tasks, dashboards, and analytics overlays.
* Manual changes to the candle timeframe or to the linked security list require a restart to ensure the subscriptions are rebuilt.
* There is no Python implementation for this strategy. Only the C# variant is included in the API package.
