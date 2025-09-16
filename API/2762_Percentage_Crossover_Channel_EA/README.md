# Percentage Crossover Channel Strategy

## Overview
The Percentage Crossover Channel strategy originates from the MetaTrader 5 expert advisor *Percentage_Crossover_Channel_EA*. It relies on a custom channel constructed around a fast moving average and reacts to either band touches or middle line crossovers. This StockSharp implementation follows the same logic while using the high-level API to process completed candles.

## Channel construction
The underlying indicator builds a dynamic channel around the selected price (close by default):

1. Compute the base price using the configured **Applied Price** mode.
2. Apply a 1-period simple moving average to obtain the short-term reference price.
3. Calculate two bounds using the **Percent** parameter (e.g., 50 → ±0.5%).
4. Clamp the previous middle line inside the new bounds to obtain the current middle value.
5. The upper and lower bands are the clamped middle value multiplied by the ±percent factors.

This recursion allows the channel to lag during strong trends while keeping a tight envelope when price consolidates.

## Trading logic
Two different signal modes are available:

- **Band touch mode (default):**
  - Long entry when the previous candle’s low was above the lower band and the last completed candle touches or pierces it.
  - Short entry when the previous candle’s high was below the upper band and the last completed candle touches or pierces it.
- **Middle crossover mode (TradeOnMiddleCross = true):**
  - Long entry when price crosses the middle line from above to below.
  - Short entry when price crosses the middle line from below to above.

The **ReverseSignals** flag swaps long and short rules. The strategy always closes and reverses existing positions by sending a single market order whose volume equals the configured **OrderVolume** plus the absolute value of the current position.

## Risk management
Optional protective levels emulate the original MT5 stop-loss and take-profit settings:

- **StopLossPoints** – distance in price steps subtracted (long) or added (short) from the estimated entry price.
- **TakeProfitPoints** – distance in price steps added (long) or subtracted (short) from the entry price.

If either parameter is zero the corresponding protection is disabled. Stops are evaluated on each finished candle by comparing candle highs and lows against the stored levels. No trailing logic is applied.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle data type to subscribe to (15-minute time frame by default). |
| `Percent` | Channel width in percent of price (converted to ±percent/100 factors). |
| `PriceMode` | Applied price for the channel. Options: Close, Open, High, Low, Median (H+L)/2, Typical (H+L+C)/3, Weighted (H+L+2C)/4, Average (O+H+L+C)/4. |
| `TradeOnMiddleCross` | Switch between band touch logic and middle line crossover logic. |
| `ReverseSignals` | Invert long and short conditions. |
| `StopLossPoints` | Protective stop distance expressed in security price steps. |
| `TakeProfitPoints` | Profit target distance expressed in security price steps. |
| `OrderVolume` | Base volume for market entries. The strategy adds the absolute open position to reverse in one transaction. |

## Implementation notes
- Orders are issued only after candles finish, which mirrors the MT5 expert that acted at the beginning of the next bar using the previous bar’s data.
- The channel indicator is recreated inside the strategy without storing historical collections, relying on scalar state variables.
- Protective stops and targets are checked manually to replicate the platform-specific order handling from MT5.
- Ensure the selected security exposes a valid `PriceStep`; otherwise stop-loss and take-profit distances will be ignored.
