# Master MM Droid Strategy

## Overview
The Master MM Droid strategy is a multi-module port of the original MetaTrader 5 expert advisor. The StockSharp implementation keeps the core ideas of the legacy robot while using the high-level API for candle subscriptions, indicator binding and order management. Four independent money-management blocks can be switched on or off, allowing the strategy to mix momentum entries with scheduled breakout orders and weekly gap plays.

## Modules
1. **RSI Block**
   - Uses a 14-period Relative Strength Index on the configured candle type.
   - Enters long when RSI crosses up from the oversold threshold and short when it crosses down from the overbought level.
   - Allows pyramiding with a configurable number of additional entries separated by a fixed price step.
   - Applies a fixed initial stop based on point distance and activates a trailing stop once the position is open.
2. **Box Breakout Block**
   - Rebuilds breakout boxes three times per day (session-shifted hours 6, 12 and 20 by default).
   - Places bracketed stop orders above the session high and below the session low with a configurable buffer.
   - Clears any pending orders and positions at session resets (hours 0, 10 and 16), mimicking the original expert behaviour.
3. **Weekly Breakout Block**
   - Tracks Monday price action and stores the running high/low of the first part of the session.
   - Places symmetrical stop orders within a limited activation window (`StartHour` – `WeeklySetupEndHour`) so the week starts with an OCO breakout.
   - Forces a flat state on Friday evenings to avoid weekend exposure.
4. **Gap Block**
   - Compares the new daily open with the previous day high/low (using the shifted calendar).
   - Buys strong gap-down openings and sells strong gap-up openings.
   - Sets a protective stop at a configurable distance and hands further management to the trailing engine.

## Parameters
| Name | Description |
| ---- | ----------- |
| `CandleType` | Time frame used for indicator calculations and time-window checks. |
| `TimeShiftHours` | Session shift applied to candle timestamps so the hourly schedule matches the original EA. |
| `StartHour` | Base Monday start hour for the weekly module (before applying the shift). |
| `EnableRsiModule`, `EnableBoxModule`, `EnableWeeklyModule`, `EnableGapModule` | Toggles for the four independent blocks. |
| `RsiPeriod`, `RsiLowerLevel`, `RsiUpperLevel` | RSI calculation and trigger levels. |
| `RsiMaxEntries`, `RsiPyramidPoints` | Pyramiding controls for the RSI block. |
| `RsiStopLossPoints`, `RsiTrailingPoints` | Initial and trailing stop sizes (in points) for RSI-driven trades. |
| `BoxEntryPoints`, `BoxTrailingPoints` | Breakout buffer and trailing distance for the box orders. |
| `WeeklyEntryPoints`, `WeeklySetupEndHour`, `WeeklyTrailingPoints` | Weekly breakout configuration. |
| `GapStopLossPoints`, `GapTrailingPoints` | Gap module protective stop and trailing distance. |

All point-based parameters are multiplied by the instrument `TickSize` to obtain price offsets so that the strategy adapts to different symbols.

## Trading Logic
- **Indicator Binding**: A single RSI indicator is bound to the candle subscription. Every finished candle triggers `ProcessCandle`, which dispatches the values to the four module handlers.
- **Daily State Tracking**: The strategy aggregates open/high/low for each shifted day to support the gap logic and to keep a historical reference for the weekly module.
- **Order Placement**: Orders are submitted through `BuyMarket`, `SellMarket`, `BuyStop`, `SellStop` in line with the high-level API best practices. Scheduled modules always cancel active orders before re-arming to avoid duplicates.
- **Trailing Management**: Once a position is active, `_activeTrailingPoints` stores the module-specific distance. The `UpdateTrailing` method moves stop orders only in the favourable direction.

## Risk Management
- Only market orders created by the RSI and gap modules are protected by an immediate stop calculated in points.
- Breakout modules rely on the trailing engine after activation; they can be combined with external portfolio protection if required.
- Calling `ClosePosition()` is the canonical way to flatten, preserving compatibility with StockSharp risk tools.

## Usage Notes
- The strategy operates on a single security and uses the global `Volume` value for sizing. Adjust portfolio protection separately if you need per-position risk limits.
- Session times are evaluated after applying the `TimeShiftHours`. For example, with the default value `2`, the box reset at hour `0` corresponds to 02:00 server time.
- Because StockSharp strategies manage net positions, simultaneous long/short baskets (possible in hedging MetaTrader accounts) are consolidated. This is the main behavioural difference from the original EA and should be considered during validation.

## Logging and Monitoring
- Each module resets its internal flags once the position returns to flat, helping operators diagnose which block produced a trade.
- Add optional charting or logging through StockSharp facilities if detailed analytics are required.
