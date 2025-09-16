# Control Panel Strategy

## Overview
The **Control Panel Strategy** ports the manual trading panel from the original MQL5 script into the StockSharp high-level API. The class exposes helper methods that replicate every button from the panel: volume preset toggles, market buy/sell actions, closing the current position, reversing exposure, and a dedicated break-even routine. Protective stop-loss and take-profit orders can be generated automatically around the average entry price, mirroring the safety features of the source expert advisor.

Instead of drawing chart controls, the StockSharp implementation provides a strongly typed interface that can be called from UI code, scripts, or automated workflows. The strategy keeps track of selected volume presets, rounds volumes to the nearest exchange step, and issues market/stop/limit orders via the built-in `Strategy` helpers such as `BuyMarket`, `SellMarket`, `SellStop`, and `BuyLimit`.

## Parameters
- **VolumeList** – semicolon separated volume presets that behave like the original checkboxes. Only the first nine values are used to stay compatible with the MQL layout. Whitespace is ignored and invalid numbers are skipped.
- **CurrentVolume** – aggregated volume based on the currently toggled presets. The setter rounds the value using `Security.VolumeStep` (when available) or two decimal places (forex-style lots). You may also set this parameter manually when integrating with an external UI.
- **BreakEvenSteps** – number of price steps added to the entry price when moving the protective stop to break-even through `ApplyBreakEven()`. If the security has no `PriceStep`, the value is treated as a direct price offset.
- **StopLossSteps** – initial stop-loss distance expressed in price steps. A value of zero disables automatic stops when a position opens or changes.
- **TakeProfitSteps** – initial take-profit distance in price steps. Works the same way as the stop-loss parameter.

## Manual Controls
All runtime actions are exposed through public methods so the host application can wire them to buttons, hotkeys, or scripts:

- `ToggleVolumeSelection(int index)` – mimics the preset checkboxes by adding or removing a volume from the aggregated amount. Invalid indexes throw to prevent silent mistakes.
- `ResetVolumeSelection()` – clears every preset and resets `CurrentVolume` to zero.
- `ExecuteBuy()` / `ExecuteSell()` – submit market orders using the current volume. Both methods return `false` when no volume is selected.
- `CloseAllPositions()` – sends a market order opposite to the current position size (`BuyMarket` for shorts, `SellMarket` for longs`).
- `ReversePosition()` – closes the existing position and immediately opens a new one in the opposite direction using the aggregated volume, exactly like the “Reverse” button in the MQL panel.
- `ApplyBreakEven()` – recalculates the protective stop as `average entry ± BreakEvenSteps * PriceStep` and places a new stop order (`SellStop` for longs, `BuyStop` for shorts`). Returns `true` only when the strategy holds an open position and an offset greater than zero is provided.

Whenever the position size changes, `OnPositionChanged` rebuilds the protective orders. First it cancels the previous stop/target pair, then it recreates them using the latest average entry price and the configured offsets. Closing the position (manually or by stop/target fills) removes any active protective orders to avoid orphaned instructions on the exchange.

## Usage Workflow
1. Configure the desired volume presets in **VolumeList** (for example `0.05; 0.10; 0.25; 0.50; 1.00`).
2. Toggle one or more presets with `ToggleVolumeSelection`. The `CurrentVolume` parameter shows the accumulated value after rounding.
3. Call `ExecuteBuy` or `ExecuteSell` to enter the market. If **StopLossSteps** or **TakeProfitSteps** are greater than zero the strategy will automatically place `SellStop`/`BuyStop` and `SellLimit`/`BuyLimit` orders relative to the average entry price.
4. Use `ApplyBreakEven` when price moves in your favor to trail the stop above (for longs) or below (for shorts) the entry by the configured offset.
5. `CloseAllPositions` exits the market immediately, while `ReversePosition` both closes and flips the exposure while reusing the currently selected lot size.
6. `ResetVolumeSelection` prepares the panel for the next trade by clearing all presets.

## Notes and Recommendations
- Break-even and protection logic relies on `PositionAvgPrice` and the current `Security.PriceStep`. Ensure the security metadata is populated before starting the strategy.
- `StartProtection()` is called during `OnStarted` so the built-in protection engine can track stop/target orders that this strategy registers.
- The helper methods are synchronous wrappers around StockSharp order helpers. Exchanges or adapters that require asynchronous confirmation should wait for order events before issuing the next command if strict sequencing is needed.
- The class can be embedded into custom WPF/WinForms panels, REST services, or console tools by mapping UI events to the exposed methods.
