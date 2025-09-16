# FATL MACD Trend Strategy
[中文](README_cn.md) | [Русский](README_ru.md)

This strategy implements a trend-following system based on the **FATL MACD** indicator. FATL (Fast Adaptive Trend Line) is subtracted from price to produce a MACD-like oscillator which is then smoothed by an adaptive moving average. Positive values indicate bullish momentum, negative values indicate bearish momentum.

The algorithm analyses the slope of this oscillator on each finished candle:

- When the previous value is lower than the value before it, the oscillator has turned upward. If the current value rises further, the strategy opens a long position and closes any short positions.
- When the previous value is higher than the value before it, the oscillator has turned downward. If the current value continues to fall, the strategy opens a short position and closes any long positions.

All main parameters are configurable:

- **Fast EMA** – MACD fast moving average period (default 12).
- **Slow EMA** – MACD slow moving average period (default 26).
- **Signal EMA** – MACD signal line period (default 9).
- **Candle Type** – candle series used for indicator calculation.

Positions are opened with market orders and are closed when an opposite signal appears.
