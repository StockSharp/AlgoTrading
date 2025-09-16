# Heiken Ashi Smoothed Trend Strategy

This strategy uses EMA-smoothed Heiken-Ashi candles to detect trend reversals. A bullish candle turning from red to green opens a long position and closes any short. Conversely, a bearish candle turning from green to red opens a short and closes any long.

- **Indicators**: Heikin-Ashi (with EMA smoothing)
- **Entry Rules**:
  - Enter long when the smoothed Heikin-Ashi candle becomes bullish.
  - Enter short when the smoothed candle becomes bearish.
- **Exit Rules**:
  - Reverse position on opposite signal.
- **Parameters**:
  - `EmaLength` – smoothing period for the EMA.
  - `CandleType` – timeframe of candles.

The algorithm recomputes the smoothed open and close for each finished candle and flips the position accordingly.
