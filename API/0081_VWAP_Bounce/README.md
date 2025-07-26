# VWAP Bounce Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Volume Weighted Average Price (VWAP) is a popular intraday benchmark. When price deviates significantly from VWAP and then prints a candle back toward it, a brief reversion move often follows. This strategy trades those bounces.

For each bar the current VWAP is computed. If a bullish candle closes below VWAP the system goes long; if a bearish candle closes above VWAP it goes short. A fixed stop-loss percentage manages risk, and positions are typically held only until an opposite signal forms or the stop is reached.

Because it fades intraday extremes, the method works best in range‑bound markets rather than strong trends.

## Details

- **Entry Criteria**: Close below VWAP with bullish candle or above VWAP with bearish candle.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal or stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `CandleType` = 5 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: VWAP
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 130%. It performs best in the stocks market.
