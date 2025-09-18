# XP Trade Manager Strategy

## Summary
- **Name**: XP Trade Manager Strategy
- **Source**: Converted from MetaTrader expert "XP Trade Manager"
- **Type**: Trade management / risk manager
- **Asset class**: FX / CFD (any instrument that exposes bid/ask and price step)

## Description
The XP Trade Manager Strategy does not generate trading signals. Instead it supervises already opened positions and pending orders of the current instrument. The module replicates the MetaTrader expert by automatically attaching stop-loss, take-profit, breakeven and trailing-stop levels while new bid/ask quotes arrive. When *Stealth Mode* is disabled the strategy maintains visible protective orders (stop and limit) that mirror the calculated risk levels. When *Stealth Mode* is enabled the strategy keeps the levels internally and exits by sending market orders once a level is touched.

The logic recognises both long and short positions and adapts the calculations to each side. Trailing-stop logic mimics the original behaviour: the stop activates only after price reaches the configured activation distance, moves in discrete steps and may optionally be capped at the breakeven level. Break-even handling moves the stop to a small profit once the activation distance is reached and trailing-stop is disabled.

## Key Features
1. Automatically calculates initial stop-loss and take-profit prices based on the entry price and user-defined point distances.
2. Optional trailing stop that moves in discrete increments and can be limited to breakeven.
3. Breakeven logic that replaces the original stop once the configured profit threshold is reached.
4. Supports both visible protective orders and fully virtual (stealth) management.
5. Uses live level1 data (bid/ask) to evaluate exits, matching the behaviour of the original MetaTrader manager.

## Parameters
| Name | Description |
|------|-------------|
| `StopLossPoints` | Distance in instrument points used to initialise the protective stop. |
| `TakeProfitPoints` | Distance in points used for the profit target. |
| `UseBreakEven` | Enables breakeven logic when trailing stop is disabled. |
| `BreakevenActivationPoints` | Profit (in points) required before the stop is moved to breakeven. |
| `BreakevenLevelPoints` | Offset in points kept after breakeven is activated. |
| `UseTrailingStop` | Enables the trailing stop subsystem. |
| `TrailingStartPoints` | Profit (in points) required before the trailing stop activates. |
| `TrailingStepPoints` | Profit increment (in points) needed to move the trailing stop again. |
| `TrailingDistancePoints` | Distance in points kept between price and the trailing stop once active. |
| `TrailingEndAtBreakeven` | Limits the trailing stop so it never exceeds the breakeven level. |
| `StealthMode` | Disables visible protective orders and performs virtual exits via market orders. |

## Usage Notes
1. Attach the strategy to the desired instrument and start it before or after positions exist. It will immediately begin supervising open trades.
2. Ensure Level1 (bid/ask) data is available, otherwise the manager will not receive price updates.
3. When *Stealth Mode* is disabled, the engine cancels and recreates protective orders if parameters change or the trailing stop advances.
4. Point distances rely on the instrument `PriceStep`; for 3 or 5 decimal FX instruments the manager uses pip-sized increments (step × 10) just like the original script.
5. The module is designed to work alongside other signal strategies that actually open positions.

## References
- Original MetaTrader source: `MQL/42180/XP Trade Manager.mq5`
