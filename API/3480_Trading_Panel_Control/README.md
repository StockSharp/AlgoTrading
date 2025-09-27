# Trading Panel Control Strategy

## Overview

The **Trading Panel Control Strategy** replicates the functionality of the "Trading Panel" MetaTrader 4 utility inside StockSharp. The original MQL panel allowed a trader to switch the active chart timeframe and to jump between instruments by clicking UI buttons. The StockSharp version exposes the same controls through strategy parameters so that the host application (Designer, Terminal, or custom dashboard) can adjust them on the fly.

Unlike the source Expert Advisor, this port does not send trading orders. Its goal is to keep the chart subscription synchronized with the currently selected timeframe and instrument and to log the latest candle closes, providing feedback similar to the text labels in the original panel.

## Key Concepts

- **Dynamic timeframe control** – choose from M1, M5, M15, M30, H1, H4, D1, or W1. Switching the parameter immediately rebuilds the candle subscription.
- **Instrument lookup** – specify a security identifier to follow. When enabled, the strategy searches the connected `ISecurityProvider`; otherwise it falls back to the security already attached to the strategy.
- **Candle feedback** – every finished candle is logged with its close price so the operator can verify the active combination of symbol and timeframe.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TimeFrameName` | Preferred timeframe code (`M1`, `M5`, `M15`, `M30`, `H1`, `H4`, `D1`, `W1`). Defaults to `M15`. |
| `SecurityId` | Optional identifier of the instrument to control. Leave blank to use the strategy's `Security` property. |
| `AutoLookupSecurity` | When `true`, the strategy resolves `SecurityId` through `SecurityProvider`. Disable it to accept the already assigned security as-is. |
| `DefaultCandleType` | Fallback `DataType` used when an unknown timeframe is entered. Defaults to one-minute candles. |

## Workflow

1. **Start-up** – on `OnStarted` the strategy resolves the target security and timeframe, then begins a candle subscription for that combination.
2. **Runtime adjustments** – changing `TimeFrameName`, `SecurityId`, or `AutoLookupSecurity` while the strategy is running restarts the subscription with the new settings.
3. **Candle processing** – each finished candle updates the `LastFinishedCandle` property and writes a log entry containing the security identifier, timeframe code, and close price.
4. **Shutdown** – subscriptions are stopped during `OnStopped` or whenever the strategy needs to rebuild them because parameters changed.

## Usage Tips

- Combine the strategy with a chart widget in StockSharp Designer to reproduce the MT4 panel workflow. Parameter editors act as buttons/combos.
- Leave `SecurityId` blank if the host already assigns a `Security` to the strategy instance.
- The log output can be connected to a UI label or console to imitate the informational labels of the original script.

## Differences from the MQL Version

- No graphical buttons; parameter changes are used instead.
- No trading actions are sent – the logic is limited to data subscription management and logging.
- The timeframe list is identical to the original panel, ensuring familiar behaviour for traders migrating from MT4.

