# Test Chart Event Strategy

## Overview
The **Test Chart Event Strategy** is a C# conversion of the MQL4 script `Test_ChartEvent.mq4`. The original script demonstrates how MetaTrader chart events work by spawning UI objects, reacting to keyboard and mouse input, and emitting custom events. This StockSharp port keeps the teaching focus: it emulates the button objects, reproduces timer-based custom events, and simulates mouse drag sessions while staying inside the strategy execution thread. No live trading orders are sent; all activity is logged to illustrate event handling.

## Core behaviour
- **Virtual button creation.** Two buttons (green and yellow) are placed on a virtual chart surface with random coordinates. The first button starts selected so that subsequent movement commands have an active target.
- **Keyboard movement emulation.** On each finished candle the strategy randomly decides whether a key press should be simulated. When triggered, horizontal and vertical direction flags mirror the arrow/NumPad logic from the MQL script. Button positions are clamped to the canvas so the objects never leave the visible area.
- **Custom timer events.** A timer in the MQL script fired every 60 seconds. Here, every completed candle plays the same role. A random event identifier (0, 1, or broadcast) is chosen and logged, reproducing `EventChartCustom` usage. Broadcast events iterate over a random number of pseudo charts to demonstrate fan-out.
- **Mouse mode.** The strategy periodically toggles a mouse-move mode to emulate pressing the `M` key. Once enabled, a press location is captured, and a later candle provides a release location. If the move lasted long enough and the displacement exceeds the pixel threshold, detailed diagnostics for the drag path and mapped price range are printed. Otherwise, the event is ignored just like the original `IsMoved` check.
- **Help and information messages.** On the first processed candle the help text and button descriptions are printed to mirror the original `H` and `I` key shortcuts.

## Parameters
| Parameter | Description | Default | Notes |
| --- | --- | --- | --- |
| `LogLevel` | Verbosity of diagnostic output (0 = silent, 1 = key actions, 2 = extended details). | 1 | Higher values show mouse press coordinates and per-event traces. |
| `MoveStep` | Pixel distance added for each simulated key press. | 10 | Matches the original `m_step` value inside `CObjectMan`. |
| `CanvasWidth` | Width of the virtual chart canvas in pixels. | 640 | Used to clamp button movement. |
| `CanvasHeight` | Height of the virtual chart canvas in pixels. | 360 | Used to clamp button movement. |
| `CandleType` | Candle subscription that drives timer-like callbacks. | 1-minute time frame | Any candle type can be supplied when running the sample. |

All parameters are exposed through `StrategyParam<T>` so they can be tuned in the StockSharp UI or optimised in research mode.

## Execution flow
1. **Start-up.** `OnStarted` creates the buttons, subscribes to candles, and announces that help will be printed automatically.
2. **First candle.** The first finished candle prints the help text and button info, just like hitting `H` and `I` in the MQL version.
3. **Per-candle loop.** Every completed candle:
   - Fires one of the three custom events and logs which identifier was chosen.
   - Potentially moves the selected button using simulated keyboard directions.
   - Occasionally switches the selection between the two buttons.
   - Handles the mouse state machine (enable → press → release) and logs drag information when the displacement/duration threshold is met.
4. **Shutdown.** `OnStopped` resets the mouse emulator to avoid dangling state.

## Logging levels
- **Level 0:** Only mandatory framework messages appear. Use this when focusing purely on performance.
- **Level 1:** Prints high-level activity such as custom events, object movement, selection changes, and successful mouse drags.
- **Level 2:** Adds low-level diagnostics including mouse press coordinates and ignored drag attempts.

## Implementation notes
- The `MouseEventEmulator` class mirrors the state machine from the MQL helper functions (`MouseEventUse`, `MouseMove`, and `IsMoved`). Duration and displacement thresholds match the original values (460 ms and three pixels respectively).
- `ChartButton` encapsulates the old `CObjectMan` responsibilities: randomised creation, clamped movement, and formatted state logging.
- The timer logic uses the candle stream to stay inside the strategy thread, avoiding external timers and thread-safety issues.
- All comments are written in English to match the repository requirements, while the user instructions are preserved in this README.

## Differences versus the MQL script
- Because StockSharp strategies do not receive live keyboard/mouse events, the interactions are simulated deterministically with randomness. The goal is educational logging rather than user-driven UI.
- The port avoids actual chart drawings for portability. Nevertheless, the positions and colours are preserved in the logs so the educational intent remains intact.
- The timer period now depends on the chosen candle type. Selecting a one-minute candle replicates the original 60-second timer.

## Usage
1. Add the strategy to a StockSharp solution and set the desired instrument/security.
2. Adjust parameters if different canvas or step sizes are needed.
3. Run the strategy in emulation or backtesting mode. Watch the log output to observe how chart events are handled.
4. Stop the strategy to reset all state; no open positions or orders will exist because the sample never trades.

This README provides the detailed context needed to understand and demonstrate chart event handling within StockSharp.
