# ATR Normalize Histogram Strategy

## Overview
The ATR Normalize Histogram strategy reproduces the behavior of the MetaTrader expert *Exp_ATR_Normalize_Histogram* inside StockSharp. The system observes the normalized ratio between the smoothed close-to-low distance and the smoothed true range. Color changes of the histogram drive both entries and exits, emulating the multi-buffer logic used in the original MQL5 implementation.

## Indicator Calculation
1. For every finished candle the strategy calculates:
   - `diff = Close − Low`.
   - `range = max(High, previous Close) − min(Low, previous Close)`.
2. Each series is smoothed independently with the selected methods and lengths. Five methods are available: Simple, Exponential, Smoothed (RMA), Weighted and Jurik. Unsupported MQL methods (JurX, Parabolic, T3, VIDYA, AMA) fall back to the simple moving average.
3. The normalized histogram value is computed as
   
   `normalized = 100 × smoothedDiff / max(|smoothedRange|, PriceStep)`.
4. Thresholds split the histogram into five bands. Crossing between bands mirrors the color buffer produced by the MQL indicator.

## Signal Logic
- **Entry filter** – `SignalBar` selects which historical bar should be evaluated (default 1, the last closed bar). The strategy compares the color of that bar with the previous one:
  - A transition from the bullish extreme (color `0`) to any other color opens a long position when long trades are enabled.
  - A transition from the bearish extreme (color `4`) to any other color opens a short position when short trades are enabled.
- **Exit filter** – the color of the previous bar alone is sufficient to close positions:
  - Color `0` closes short positions if short exits are enabled.
  - Color `4` closes long positions if long exits are enabled.
- Exits are processed before any new entries so that the strategy never keeps overlapping trades.

## Risk Management
The strategy keeps track of the last fill price and optionally enforces protective stops and targets measured in instrument points. The conversion uses `Security.PriceStep`, matching the “points” concept from the original expert. When either limit is hit intrabar, the position is closed immediately and trade direction can change on the following signal.

## Parameters
- `CandleType` – timeframe used for the calculation.
- `FirstSmoothingMethod` / `SecondSmoothingMethod` – smoothing type for `diff` and `range` streams.
- `FirstLength` / `SecondLength` – periods for the smoothers.
- `HighLevel`, `MiddleLevel`, `LowLevel` – histogram thresholds (default 60/50/40).
- `SignalBar` – offset for buffer evaluation (minimum 1).
- `EnableBuyEntries`, `EnableSellEntries`, `EnableBuyExits`, `EnableSellExits` – toggles for managing the four trade directions.
- `TradeVolume` – base order size. The strategy automatically offsets existing exposure when flipping direction.
- `StopLossPoints`, `TakeProfitPoints` – optional protective distances in points; set to zero to disable.

## Notes and Differences vs. MQL Version
- Both smoothing stages are configurable independently, but only the five StockSharp moving average implementations are available. When another MQL method is selected the strategy defaults to the simple moving average while keeping the length.
- The `SignalBar` logic follows the buffer shift used in `CopyBuffer`, so larger offsets still compare the chosen bar with its immediate predecessor.
- Money management parameters from the original expert (`MM`, `MMMode`, `Deviation`) are simplified to a single `TradeVolume` parameter. Order execution happens at market with optional stop/target monitoring.
