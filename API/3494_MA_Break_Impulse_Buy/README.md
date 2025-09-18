# MA Break Impulse Buy Strategy

## Overview
This strategy reproduces the "M.A break mt4 buy" expert advisor using StockSharp's high-level API. It focuses on identifying strong bullish breakouts after a quiet consolidation. The entry logic looks for a sequence of exponential moving average (EMA) filters, a quiet market phase, and then a powerful bullish impulse candle that interacts with a breakout EMA. The strategy opens **long** positions only.

## Trading Logic
1. **EMA Trend Filters**
   - Two EMA pairs are evaluated on the previous completed candle (`shift = 1`).
   - `EMA(FirstFastPeriod)` must be greater than `EMA(FirstSlowPeriod)`.
   - `EMA(SecondFastPeriod)` must be greater than `EMA(SecondSlowPeriod)`.
2. **Impulse Candle Selection**
   - The impulse candle is the last completed bar (shift 1).
   - Its open price must be above the `TrendMaPeriod` EMA.
   - Its low must touch or fall below the `BreakoutMaPeriod` EMA.
   - The candle must be bullish (`Close > Open`).
   - Candle range must lie between `CandleMinSize` and `CandleMaxSize` (converted from pips using `Security.PriceStep`).
   - Upper wick must not exceed `UpperWickLimit` percent of the candle range. The lower wick must be at least `LowerWickFloor` percent of the range.
3. **Quiet Bars and Impulse Strength**
   - The strategy scans `QuietBarsCount` candles preceding the impulse candle (shifts ≥ 2) and records the maximum high-low range.
   - This quiet range must be greater than `QuietBarsMinRange` (pips → price).
   - The body of the impulse candle (`Close - Open`) must be at least `ImpulseStrength × quietRange`.
4. **Position Management**
   - A market buy order is sent when all conditions are met and no position is currently open.
   - Protective stop-loss and take-profit orders are managed through `StartProtection`, using pip inputs converted via `Security.PriceStep`.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `FirstFastPeriod` | 20 | Fast EMA used in the first trend filter. |
| `FirstSlowPeriod` | 30 | Slow EMA used in the first trend filter. |
| `SecondFastPeriod` | 30 | Fast EMA for the second trend filter. |
| `SecondSlowPeriod` | 50 | Slow EMA for the second trend filter. |
| `TrendMaPeriod` | 30 | EMA that the impulse candle open must exceed. |
| `BreakoutMaPeriod` | 20 | EMA that the impulse candle low must touch. |
| `QuietBarsCount` | 2 | Number of calm candles before the impulse is evaluated. |
| `QuietBarsMinRange` | 0.0 | Minimum quiet range (pips). |
| `ImpulseStrength` | 1.1 | Multiplier applied to the quiet range to validate the impulse body size. |
| `UpperWickLimit` | 100.0 | Maximum upper wick as percent of candle range. |
| `LowerWickFloor` | 0.0 | Minimum lower wick as percent of candle range. |
| `CandleMinSize` | 0.0 | Minimum allowed range of the impulse candle in pips. |
| `CandleMaxSize` | 100.0 | Maximum allowed range of the impulse candle in pips. |
| `VolumeSize` | 0.01 | Trade volume sent with `BuyMarket`. Normalized to exchange `VolumeStep`. |
| `StopLossPips` | 20.0 | Stop-loss distance in pips (converted with `PriceStep`). |
| `TakeProfitPips` | 20.0 | Take-profit distance in pips (converted with `PriceStep`). |
| `CandleType` | 15-minute time frame | Candle data type requested from the connector. |

## Implementation Notes
- The strategy uses StockSharp high-level `Bind` subscriptions to keep indicator calculations in sync with candle updates.
- All calculations rely on finished candles only (`CandleStates.Finished`).
- Quiet-range and candle-size filters internally convert pip values into price units using `Security.PriceStep`. If the instrument does not report `PriceStep`, a fallback of `1` is used, matching the MQL logic of multiplying by pip value.
- `StartProtection` is activated once during `OnStarted` so every new position receives the configured stop-loss and take-profit.
- The candle history buffer keeps only the last `QuietBarsCount + 3` entries to evaluate the quiet period and impulse candle efficiently.

## Usage Tips
- Make sure the connected instrument provides `PriceStep`, `VolumeStep`, and volume limits so that pip and volume conversions remain accurate.
- Adjust EMA periods and impulse parameters to the instrument's volatility. Lower `ImpulseStrength` will react to smaller breakouts, while a higher value filters only the strongest moves.
- The strategy is designed for one open position at a time. External positions on the same security may prevent new entries.

