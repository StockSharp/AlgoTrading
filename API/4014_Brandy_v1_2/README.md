# Brandy v1.2 Strategy (C#)

## Overview
The **Brandy v1.2 Strategy** is a direct conversion of the MetaTrader 4 expert advisor "Brandy_v1_2.mq4" into the StockSharp high-level strategy framework. The system evaluates a pair of displaced simple moving averages (SMAs) calculated on the closing price of the configured candle series. New positions are opened only when both the long-term and short-term SMAs show synchronized momentum in the same direction, while existing trades are managed using slope reversals, fixed stop-loss levels, and an optional trailing stop module.

The original MQL script executed exactly once per completed bar. This port processes finished StockSharp candles in the same fashion, ensuring that all trading decisions are based on closed data without relying on partially formed bars.

## Trading Logic
1. **Indicator preparation**
   - Two SMAs are computed: a longer baseline (`LongPeriod`) and a shorter confirmation line (`ShortPeriod`).
   - Each average is accessed twice: the value from the previous bar (shift = 1) and another value displaced by `LongShift`/`ShortShift` bars respectively. This reproduces the `iMA(..., shift)` calls present in the original EA.
2. **Entry rules**
   - **Buy** when the previous-bar value of both SMAs is greater than their shifted counterparts (both slopes pointing upward) and no position is open.
   - **Sell** when the previous-bar value of both SMAs is lower than their shifted counterparts (both slopes pointing downward) and no position is open.
   - Only one position can be active at any time, mirroring the `k == 0` check in the MQL source.
3. **Exit rules**
   - **Slope reversal**: an open long position is liquidated if the long SMA turns down (`longPrev < longShifted`), while a short position is covered when the long SMA turns up (`longPrev > longShifted`).
   - **Fixed stop-loss**: upon entering, the strategy stores an initial stop level offset by `StopLossPoints × PriceStep` from the entry price. The stop is checked against the candle’s high/low range, approximating the tick-level management of the original advisor.
   - **Trailing stop**: if `TrailingStopPoints ≥ 100`, the strategy replicates the trailing logic (`ts` parameter). Once the floating profit exceeds the trailing distance, the stop is pulled to `currentPrice ± trailingDistance`, provided the new level is closer to price than the existing stop. This behavior matches the `OrderModify` calls in the MQL expert.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `LongPeriod` | 70 | Length of the primary SMA (`p1` in MQL). Must be > 0. |
| `LongShift` | 5 | Backward shift applied to the long SMA comparison (`s1`). Can be zero. |
| `ShortPeriod` | 20 | Length of the confirmation SMA (`p2`). Must be > 0. |
| `ShortShift` | 5 | Backward shift for the short SMA (`s2`). Can be zero. |
| `StopLossPoints` | 50 | Fixed stop distance in price steps (`sl`). Set to 0 to disable the hard stop. |
| `TrailingStopPoints` | 150 | Trailing distance in price steps (`ts`). Trailing activates only when the value is ≥ 100, mirroring the original threshold. |
| `Volume` | 0.1 | Order volume used for entries (`lots`). |
| `CandleType` | 15-minute time frame | Candle series processed by the strategy (user configurable). |

### Price step dependency
Both stop parameters operate in instrument points. The helper method converts them to absolute price deltas via `Security.PriceStep`. If the data source does not supply `PriceStep`, the strategy falls back to `0.0001` so the logic continues to work, albeit with an approximate conversion. Always verify the symbol metadata in StockSharp before live usage.

## Risk Management
- **Hard stop**: stored internally and validated against every finished candle. When price violates the stop, the corresponding `SellMarket`/`BuyMarket` call closes the entire position.
- **Trailing stop**: follows the exact conditions of the original EA, moving the stop only when the current profit exceeds the trailing distance *and* the existing stop is still farther than that distance.
- **Single position**: the algorithm never pyramids; it either has a single long position, a single short position, or is flat.

## Implementation Notes
- State (entry price, stop level, SMA histories) resets automatically on `OnReseted()` ensuring clean backtests and restarts.
- Indicator histories are stored in short rolling buffers to reproduce the `iMA(..., shift)` offsets without calling `GetValue()`.
- All inline comments remain in English as required by the repository guidelines.
- No Python counterpart is provided. Only the C# high-level implementation is delivered in `CS/BrandyV12Strategy.cs` as requested.

## Usage
1. Place the strategy into a StockSharp solution, select the desired instrument, and ensure the candle data matches the timeframe specified by `CandleType`.
2. Configure the parameters in the UI or via code. Defaults replicate the original MT4 values.
3. Start the strategy. It will subscribe to the candle series, draw both SMAs on the chart, and manage trades automatically.

> **Disclaimer:** This port is intended for educational and testing purposes. Always validate the behavior on historical and paper trading sessions before deploying to live markets.
