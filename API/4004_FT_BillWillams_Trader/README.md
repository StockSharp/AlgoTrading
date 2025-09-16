# FT Bill Williams Trader Strategy

## Overview

The **FT Bill Williams Trader Strategy** is a high-level StockSharp translation of the MetaTrader expert advisor "FT_BillWillams_Trader". It combines Bill Williams fractals with the Alligator indicator to trade trend breakouts. The strategy watches for fresh fractals, verifies that the Alligator structure confirms the breakout direction, and optionally applies distance, alignment, and reverse-signal filters before opening a position.

## Trading Logic

1. **Fractal detection** – the strategy buffers the most recent `FractalPeriod` highs and lows. When the middle bar is the highest (or lowest) point in the window, a new breakout level is recorded. An `IndentPoints` offset is added above/below the fractal to avoid premature entries.
2. **Breakout confirmation** – depending on `EntryConfirmation`:
   - `PriceBreakout` confirms when the candle range crosses the breakout level.
   - `CloseBreakout` waits for the previous candle close to be beyond the level.
3. **Distance check** – entries are rejected when the breakout level is farther than `MaxDistancePoints` from the Alligator lips (previous bar value). Set the distance to zero to disable the filter.
4. **Teeth filter** – when `UseTeethFilter` is enabled, the previous close must be above (for longs) or below (for shorts) the Alligator teeth.
5. **Trend alignment** – with `UseTrendAlignment = true`, the lips, teeth, and jaw must be separated by at least `TeethLipsDistancePoints` and `JawTeethDistancePoints` points, respectively, confirming that the Alligator is trending.
6. **Reverse exits** – if `ReverseExit = OppositeFractal`, any new opposite fractal immediately closes the open position. With `OppositePosition`, the strategy first closes the current trade before opening one in the opposite direction.
7. **Jaw exit** – `JawExit` defines whether the position is closed when price crosses the Alligator jaw (intrabar or on candle close).
8. **Trailing stop** – when `EnableTrailing` is true and the trade is profitable, the stop moves to the lips or teeth depending on the relative slope of the lips and the `SlopeSmaPeriod` SMA. Initial protective stops and profit targets are controlled by `StopLossPoints` and `TakeProfitPoints`.

## Parameters

| Property | Description | Default |
|----------|-------------|---------|
| `OrderVolume` | Trade volume used when sending market orders. | `0.1` |
| `FractalPeriod` | Number of bars in the fractal pattern (odd values recommended). | `5` |
| `IndentPoints` | Offset added to the breakout level (in points). | `1` |
| `EntryConfirmation` | Breakout confirmation mode (`PriceBreakout`, `CloseBreakout`). | `CloseBreakout` |
| `UseTeethFilter` | Require the previous close to be on the correct side of the Alligator teeth. | `true` |
| `MaxDistancePoints` | Maximum distance between breakout level and Alligator lips (points). | `1000` |
| `UseTrendAlignment` | Enforce minimum separation between Alligator lines. | `false` |
| `JawTeethDistancePoints` | Minimum jaw-to-teeth distance used in the alignment filter. | `10` |
| `TeethLipsDistancePoints` | Minimum teeth-to-lips distance used in the alignment filter. | `10` |
| `JawExit` | Mode for closing positions on jaw crossover (`Disabled`, `PriceCross`, `CloseCross`). | `CloseCross` |
| `ReverseExit` | Opposite-signal handling (`Disabled`, `OppositeFractal`, `OppositePosition`). | `OppositePosition` |
| `EnableTrailing` | Enable Alligator-based trailing stop management. | `true` |
| `SlopeSmaPeriod` | Period of the SMA that is compared with the lips slope. | `5` |
| `StopLossPoints` | Stop-loss distance in points (0 disables). | `50` |
| `TakeProfitPoints` | Take-profit distance in points (0 disables). | `50` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Periods for the Alligator lines. | `13`, `8`, `5` |
| `JawShift`, `TeethShift`, `LipsShift` | Forward shift for each Alligator line. | `8`, `5`, `3` |
| `MaMethod` | Moving-average type for the Alligator (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Simple` |
| `AppliedPrice` | Candle price supplied to the Alligator. | `CandlePrice.Median` |
| `CandleType` | Candle type subscribed from the market data. | `15-minute timeframe` |

## Additional Notes

- The strategy draws the Alligator lines and executed trades on the default chart area.
- `FractalPeriod` should remain odd so that the middle bar represents the fractal apex; the default value matches the original expert advisor.
- Distance-based parameters (`IndentPoints`, `MaxDistancePoints`, `JawTeethDistancePoints`, `TeethLipsDistancePoints`, `StopLossPoints`, `TakeProfitPoints`) are expressed in broker points (`Security.PriceStep`).
- Trailing stops and jaw exits rely on completed candles, mirroring the original MQL logic that works with the previous bar values of the Alligator.
