# Trade Panel With Autopilot Strategy

This strategy ports the MQL5 **Trade panel with autopilot** example to the StockSharp framework.
It calculates bullish and bearish pressure across multiple time frames. A position is opened when the corresponding percentage exceeds the *Open %* threshold and closed when it falls below the *Close %* level. Optionally a fractal based stop loss can be applied using 10‑minute candles.

## Parameters

- **Autopilot** – enable or disable automated trading.
- **Open %** – threshold of votes required to open a position.
- **Close %** – threshold for closing existing position.
- **Use Fixed Volume** – if true, use the value from *Fixed Volume*.
- **Fixed Volume** – absolute order volume.
- **Volume %** – portfolio percentage used when volume is dynamic.
- **Use Stop Loss** – enable stop loss based on recent fractals.

## Logic

For every time frame from 1 minute to 1 month the strategy compares the latest candle with the previous one. Each comparison of open, high, low and derived averages adds a vote for buying or selling. The percentages of buy and sell votes drive order placement. When enabled, the last fractal from 10‑minute candles acts as a trailing stop.

This example is intended for educational purposes and does not represent trading advice.
