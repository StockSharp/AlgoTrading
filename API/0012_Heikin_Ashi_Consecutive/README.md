# Heikin Ashi Consecutive

Strategy based on consecutive Heikin Ashi candles

Heikin Ashi Consecutive waits for several same-color Heikin Ashi candles to confirm momentum. After a run of bullish or bearish bars the strategy joins the move and exits on the first opposite candle or an ATR stop.

Because Heikin Ashi charts smooth price data, a series of like-colored candles highlights a strong directional move. The trailing ATR stop attempts to lock in gains if the sequence abruptly reverses.


## Rules

- **Entry Criteria**: Signals based on Heikin.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `ConsecutiveCandles` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Heikin
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
