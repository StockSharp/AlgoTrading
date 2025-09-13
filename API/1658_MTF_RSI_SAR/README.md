# MTF RSI SAR Strategy

This strategy combines **Relative Strength Index (RSI)** readings across four timeframes, **Parabolic SAR**, and **Bollinger Bands** to capture trend continuation after short pullbacks. Signals are generated on 5‑minute candles while higher timeframes act as confirmation filters.

## Concept

1. **RSI filter** – 5, 15, 30 and 60 minute RSI values must all be above 50 for long entries or below 50 for short entries. This multi‑timeframe confirmation aims to align trades with the broader trend.
2. **Parabolic SAR filter** – Parabolic SAR values on 5, 15 and 30 minute charts must be below the current candle for longs or above for shorts. This ensures that price is trending in the desired direction.
3. **Bollinger Band trigger** – On the 5‑minute chart the candle close must break the upper band for longs or the lower band for shorts. Bollinger Bands provide an overbought/oversold trigger.
4. **Entry and exit** – A long position is opened when all active filters point up. A short position is opened when all active filters point down. The opposite signal closes an open position.

Any of the three filters can be individually disabled via parameters, allowing the strategy to operate with RSI only, Bollinger Bands only, SAR only, or any combination of the above.

## Parameters

- `UseRsi` – enable RSI filter (default: true).
- `UseBollinger` – enable Bollinger Band trigger (default: true).
- `UseSar` – enable Parabolic SAR filter (default: true).
- `RsiPeriod` – RSI calculation period (default: 14).
- `BollingerPeriod` – number of bars for Bollinger Bands (default: 20).
- `BollingerWidth` – width (standard deviation multiplier) for Bollinger Bands (default: 2).
- `SarStep` – acceleration factor for Parabolic SAR (default: 0.02).
- `SarMax` – maximum acceleration factor for Parabolic SAR (default: 0.2).
- `CandleType` – base candle timeframe, 5 minutes by default.

## Trading Rules

- **Long**: all enabled filters provide bullish signals.
- **Short**: all enabled filters provide bearish signals.
- **Exit**: opposite signal closes the position.

## Notes

- Strategy operates on one security with four candle subscriptions: 5, 15, 30 and 60 minute timeframes.
- Designed as an educational example of multi‑timeframe confirmation using StockSharp's high‑level API.
- There are no fixed stop‑loss or profit targets; risk management should be added externally if required.

