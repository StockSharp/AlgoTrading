# TSI MACD Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implements a crossover system based on the True Strength Index (TSI) and its exponential moving average signal line.

The strategy subscribes to 4-hour candles by default and calculates the TSI using configurable short and long smoothing lengths. An additional EMA produces the signal line. A long position is opened when the TSI crosses above the signal line; a short position is opened when the TSI crosses below the signal line. Opposite positions are closed automatically on the reverse cross.

- Indicators: True Strength Index, Exponential Moving Average
- Parameters:
  - `CandleType` – candle series to process.
  - `LongLength` – long smoothing period for TSI.
  - `ShortLength` – short smoothing period for TSI.
  - `SignalLength` – period of the EMA signal line.
