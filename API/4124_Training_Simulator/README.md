# Training Simulator Strategy

## Overview
The Training Simulator strategy is a C# port of the MetaTrader 4 expert advisor `Training2.mq4`. The original script offered a
manual training panel: the trader dragged on-chart labels to open or close buy/sell orders, modify protective levels, and pause
the tester when custom breakpoints were reached. The StockSharp implementation preserves this workflow through strategy
parameters. It exposes explicit toggles for long/short entries, recalculates stop-loss and take-profit distances on demand, and
monitors optional upper/lower price barriers that can halt the strategy automatically.

The port assumes a netted position model (single aggregated position per instrument) and relies on StockSharp's high-level API
to submit market orders, close exposures, and stream trade prices for breakpoint detection.

## Trading Logic
1. **Manual entry toggles**
   - `EnableBuy` toggled to `true` submits a market buy order with the configured `Volume`. Switching it back to `false` closes
     any existing long position. Enabling the long toggle automatically clears the short toggle to avoid conflicting commands.
   - `EnableSell` behaves symmetrically for short positions. When both toggles are set to `false` the strategy remains flat.
2. **Protective distances**
   - `StopLossPoints` and `TakeProfitPoints` represent MetaTrader "points" (price steps). They are converted to monetary
     distances using the security's `PriceStep`. Once a position is opened the distances are stored and evaluated against every
     incoming trade. If price breaches the configured stop or target the strategy logs the event and issues a closing market
     order.
   - `ModifyLongTargets` and `ModifyShortTargets` allow on-the-fly recalculation of the active distances. Setting the flag to
     `true` applies the current parameter values to the open position and immediately resets the flag to `false`.
3. **Breakpoint monitoring**
   - Optional `UpperStopPrice` and `LowerStopPrice` mimic the MT4 "upper/lower stop" labels. When the last trade price crosses
     one of these levels the strategy logs an informational message and, if `PauseOnBreakpoint` is enabled (default), stops
     itself to emulate the MetaTrader tester pause.
4. **State management**
   - The strategy subscribes to real-time trades and runs a 250 ms timer to poll the manual toggles. Internal flags prevent
     duplicate order submissions while an exit request is already in-flight. When the position returns to flat all control
     toggles are reset to `false`, reproducing how the original panel reset its labels.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Volume` | Lot size submitted with every market order. | `0.1` |
| `TakeProfitPoints` | Distance above the entry used to detect long/short profit targets (MetaTrader points). | `30` |
| `StopLossPoints` | Distance away from the entry used to detect protective stops (MetaTrader points). | `30` |
| `UpperStopPrice` | Optional upper breakpoint price that triggers a pause/log when crossed. Set to `0` to disable. | `0` |
| `LowerStopPrice` | Optional lower breakpoint price that triggers a pause/log when crossed. Set to `0` to disable. | `0` |
| `PauseOnBreakpoint` | Stops the strategy automatically once a breakpoint fires. | `true` |
| `EnableBuy` | Manual toggle that opens/closes the long position. | `false` |
| `EnableSell` | Manual toggle that opens/closes the short position. | `false` |
| `ModifyLongTargets` | Reapplies stop/target distances to an existing long position. Resets to `false` after execution. | `false` |
| `ModifyShortTargets` | Reapplies stop/target distances to an existing short position. Resets to `false` after execution. | `false` |

## Implementation Notes
- Price updates come from `SubscribeTrades().Bind(ProcessTrade).Start()`, matching the tick-driven behaviour of the MT4 script.
- The manual controls are polled via `Timer.Start(TimeSpan.FromMilliseconds(250), ProcessManualControls);` to keep the strategy
  responsive without busy waiting.
- `AdjustVolume` normalizes the requested lot size using `VolumeStep`, `MinVolume`, and `MaxVolume`, ensuring submitted orders
  respect the instrument's trading rules.
- Stop-loss and take-profit checks rely on the current `PositionPrice` when available; otherwise the most recent trade price is
  used as a fallback for immediate fills.
- Breakpoint handling sets an internal flag so that the stop behaviour executes only once per crossing, preventing repeated
  `Stop()` calls on every tick beyond the level.

## Differences vs. the MQL Version
- Chart labels were replaced by boolean parameters because StockSharp does not expose the MetaTrader GUI drawing primitives.
- MetaTrader allowed simultaneous buy and sell tickets in hedge mode; the StockSharp port uses a single net position per
  security, which matches the platform's default behaviour.
- Instead of sending a `Pause` keystroke to the tester, breakpoint events optionally call `Stop()` on the strategy and always
  produce a descriptive log entry.
- Protective distances are evaluated internally rather than via MT4 order modification calls; this keeps the logic broker
  agnostic and avoids dependencies on `OrderModify` semantics.
