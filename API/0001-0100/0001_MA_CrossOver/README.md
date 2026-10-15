# Moving Average Crossover Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The strategy follows the relationship between a fast and a slow exponential moving average. A bullish crossover opens or reverses into a long position, while a bearish crossover opens or reverses into a short position. Signals are evaluated on finished candles.

## Details

- **Long entry**: the fast EMA crosses above the slow EMA.
- **Short entry**: the fast EMA crosses below the slow EMA.
- **Exit**: an opposite crossover reverses the position; a percentage stop-loss can close it earlier.
- **Default values**:
  - `FastLength` = 100
  - `SlowLength` = 400
  - `StopLossPercent` = 2
  - `CandleType` = 1 minute
- **Implementations**: C# and Python.
