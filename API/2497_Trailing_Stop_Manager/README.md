# Trailing Stop Manager Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy recreates the trailing stop controller from the MetaTrader expert `MQL/17263/TrailingStop.mq5`. It focuses on automating stop-loss management after an entry has already been opened.

## Original Idea
- **Source**: Vladimir Karputov's TrailingStop expert for hedging accounts.
- **Concept**: On the first tick the EA opened both long and short positions, then tightened stop-loss levels independently for each side using pip-based distances.
- **Goal**: Demonstrate how to trail stops with a configurable activation distance and update step.

## StockSharp Adaptation
- **Netting compatibility**: StockSharp strategies operate on the net position, so this port manages one direction at a time. To trail both sides simultaneously start two strategy instances.
- **Tick-based updates**: The strategy subscribes to trade ticks (`DataType.Ticks`) to mirror the tick-driven adjustments from MetaTrader.
- **Pip conversion**: It multiplies the configured pip values by `Security.PriceStep` (falls back to 1 if the exchange does not provide a step) to convert inputs into absolute price offsets.
- **Optional auto-entry**: A parameter lets you send an immediate market order on start, which is handy for quick demonstrations or manual testing.

## Trading Logic
1. **Start-up**
   - Reads the instrument price step and subscribes to tick data.
   - Optionally submits a market order according to the `Initial Direction` parameter.
2. **Entry tracking**
   - Every own trade resets the trailing state and stores the actual fill price as the new reference.
3. **Activation**
   - For long positions the trailing engine activates only after price advances by `Trailing Stop (pips)` from the entry. For shorts it requires an equivalent drop.
4. **Stop adjustment**
   - Once activated the stop level equals the current tick price minus/plus the activation distance.
   - The stop is moved only if the latest tick pushes it forward by at least `Trailing Step (pips)`.
   - A zero step means the stop is updated on every favorable tick.
5. **Exit**
   - When price returns to or beyond the trailing level the strategy closes the remaining position using a market order.

## Parameters
| Name | Description |
| --- | --- |
| **Trailing Stop (pips)** | Activation distance in pips. Must be greater than zero. |
| **Trailing Step (pips)** | Minimal favorable move in pips before the stop is advanced again. Can be zero. |
| **Initial Direction** | Optional market order placed during `OnStarted` (`None`, `Long`, `Short`). |

## Additional Notes
- The original expert used bid/ask values. This C# version uses the latest trade price as a close approximation, which is sufficient for most liquid instruments.
- No take-profit or new entry logic is included. You can combine this component with another signal strategy or launch it manually after opening a position.
- If the broker provides fractional pip steps ensure that `Security.PriceStep` reflects them; otherwise adjust the pip values to match the actual tick size.
- There are no automated tests for this module, so validate on a demo feed before deploying live capital.
