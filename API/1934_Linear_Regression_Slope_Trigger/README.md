# Linear Regression Slope Trigger Strategy

## Overview
This strategy uses a linear regression slope indicator and a derived trigger line to identify trend changes. A long position is opened when the trigger line crosses above the slope line, while a short position is opened when the trigger line crosses below the slope line. Existing positions are closed when an opposite signal appears. The approach is inspired by the original MQL5 strategy "Exp_LinearRegSlopeV2".

## Indicator Logic
1. **Linear Regression Slope** is calculated on candle close prices over a configurable period.
2. A **trigger line** is computed as `2 * slope - slope[Shift]`, where `slope[Shift]` is the slope value from several bars ago.
3. Crossovers between the trigger and slope lines serve as trading signals.

## Trading Rules
- **Enter Long:** Trigger crosses above slope and short trades are allowed.
- **Enter Short:** Trigger crosses below slope and long trades are allowed.
- **Exit Long:** Slope rises above trigger.
- **Exit Short:** Trigger rises above slope.

## Parameters
- `SlopeLength` – Period for calculating the linear regression slope.
- `TriggerShift` – Number of bars used to calculate the trigger line.
- `EnableLong` – Allows long entries.
- `EnableShort` – Allows short entries.
- `TakeProfitPercent` – Take‑profit as a percentage of entry price.
- `StopLossPercent` – Stop‑loss as a percentage of entry price.
- `CandleType` – Timeframe of candles used by the strategy.

## Notes
- The strategy operates on completed candles only.
- Protection via `StartProtection` applies fixed percent-based take‑profit and stop‑loss levels.
- Ensure sufficient historical data so the indicator can form its values.
