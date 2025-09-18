# Symbol Swap Panel Strategy

## Overview
The **Symbol Swap Panel Strategy** is a StockSharp conversion of the MQL panel *"Symbol Swap Panel"*. The original expert acted as a chart widget that allowed traders to type a symbol, switch the active chart to that symbol, and monitor real-time market information such as OHLC values, tick volume, and spread. The converted strategy recreates the same workflow in the StockSharp environment. It can be launched on any security and provides a manual toggle to jump to another instrument while continuously logging the most relevant market metrics.

## Core behaviour
- Subscribes to candle data and level-one quotes for the active security.
- Logs every completed candle with open, high, low, close, total volume, and the latest computed spread.
- Stores bid/ask quotes and derives an up-to-date spread that mirrors the MQL panel readout.
- Reacts to manual swap requests and replaces the monitored security with the chosen identifier without requiring the strategy to restart.
- Maintains the previously selected security so that redundant swaps are ignored and accidental double activations do not disrupt the subscriptions.

## Parameters
| Name | Type | Description |
| --- | --- | --- |
| `TargetSecurityId` | `string` | Security identifier that should be activated when the swap request is triggered. Empty strings are ignored with a warning. |
| `CandleType` | `DataType` | Candle aggregation for periodic updates (defaults to 1-hour candles, replicating the MQL panel timeframe). |
| `SwapRequested` | `bool` | Manual flag that requests an immediate switch to `TargetSecurityId`. It resets to `false` after the swap attempt is processed. |

## Data subscriptions
- Candle subscription created with `CandleType` for the currently active security.
- Level-one subscription used to track bid/ask quotes and compute a live spread value.
- Subscriptions are safely restarted whenever the security changes, ensuring stale data streams are not left running.

## Workflow
1. When the strategy starts it resolves the initial security from `Strategy.Security` or, if missing, from `TargetSecurityId`.
2. Candle and level-one subscriptions are opened for that instrument.
3. Each completed candle triggers a detailed log message that mirrors the text shown in the original panel labels.
4. Incoming level-one updates refresh the cached bid/ask values.
5. Setting `SwapRequested` to `true` and supplying a valid `TargetSecurityId` immediately switches the monitored security and restarts the subscriptions.

## Usage notes
- The strategy is designed for manual monitoring and does not place orders.
- The spread is only reported when both bid and ask values are present and positive.
- When an invalid or unknown symbol is provided, a warning is logged and the request is discarded without interrupting the running subscriptions.
- Because the original tool refreshed the UI once per second, you can lower the candle timeframe if you need more frequent log updates.

## Original MQL features preserved
- Manual symbol switching through a textual identifier.
- Real-time display of OHLC values, volume, and spread for the chosen symbol.
- Safeguards against empty inputs and failed Market Watch additions (translated into StockSharp warnings).

## Differences from the MQL implementation
- The StockSharp strategy uses log messages instead of on-screen labels. This matches the typical workflow inside StockSharp while still exposing the same information.
- Chart switching is implemented by reassigning the strategy security and recreating subscriptions instead of altering a terminal chart window.
- Timer-based refresh logic is replaced by candle completion events to stay aligned with high-level StockSharp APIs.

## Requirements
- StockSharp connector with access to the desired securities.
- Level-one data feed to obtain bid/ask quotes for spread calculation.
