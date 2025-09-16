# Ichi Oscillator Strategy

## Overview
- Conversion of the MetaTrader 5 expert **Exp_ICHI_OSC** into the StockSharp high-level API.
- Trades on a configurable candle series and derives signals from an oscillator built on Ichimoku lines.
- The raw oscillator value is `((Close - SenkouA) - (Tenkan - Kijun)) / Step`, smoothed by a selectable moving average.
- Orders are executed with the strategy volume; complex money-management blocks from the original code were replaced by StockSharp position handling.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Candle timeframe used for all indicator calculations. |
| `IchimokuBase` | Base period that defines the Tenkan (`base * 0.5`), Kijun (`base * 1.5`) and Senkou B (`base * 3`) lengths. |
| `Smoothing Method` | Moving average used to smooth the oscillator. Options: `Simple`, `Exponential`, `Smoothed`, `Weighted`, `Jurik`, `Kaufman`. |
| `Smoothing Length` | Period of the selected smoothing method. |
| `Smoothing Phase` | Reserved compatibility parameter (kept from MQL version, currently not used by the built-in smoothing implementations). |
| `Signal Bar` | Number of bars back from the latest finished candle used to read oscillator colors (default `1`). |
| `Enable Buy Entries / Enable Sell Entries` | Allow opening long or short positions respectively. |
| `Enable Buy Exits / Enable Sell Exits` | Allow closing existing long or short positions. |
| `Stop Loss (points)` | Protective stop distance expressed in price steps. |
| `Take Profit (points)` | Take-profit distance expressed in price steps. |
| `Order Volume` | Base order volume used by market orders. |

## Trading Logic
1. Subscribe to the requested candle series and calculate Tenkan, Kijun and Senkou A values using the derived Ichimoku periods.
2. Build the oscillator from the differences between price, Senkou A, Tenkan and Kijun and pass it through the selected smoother.
3. Assign a color code to each smoothed value:
   - `0` — oscillator above zero and rising.
   - `1` — oscillator above zero and falling.
   - `2` — neutral (zero level or unchanged).
   - `3` — oscillator below zero and decreasing.
   - `4` — oscillator below zero and rising.
4. Read two colors: the bar at `SignalBar + 1` (previous color) and the bar at `SignalBar` (current color).
   - If the previous color is `0` or `3`, close shorts when allowed and open a long when the current color is `2`, `1` or `4`.
   - If the previous color is `4` or `1`, close longs when allowed and open a short when the current color is `0`, `1` or `3`.
5. Orders are placed with the configured volume. Longs and shorts are never stacked: open signals are evaluated only after exit logic has run in the same bar.

## Risk Management
- Protective orders are managed through `StartProtection`, using the stop-loss and take-profit distances in price steps.
- No trailing or partial exits are enabled by default.

## Notes
- The original money-management module (lot calculations, deviation handling, trade timers) is replaced by StockSharp's position and volume control.
- Smoothing methods that do not exist in StockSharp (e.g., JurX, ParMA, VIDYA, T3) are not available; choose the closest alternative from the provided list.
- Signal timestamps in the logs include the candle close time plus one full candle period, mirroring the MQL `TimeShiftSec` usage.
