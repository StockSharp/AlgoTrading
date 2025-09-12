# Supertrend + MACD Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines the Supertrend indicator with a MACD crossover to identify bullish entries.
A long position is opened when price is above the Supertrend line and the MACD line crosses above its signal line.
The position is closed when price falls below the Supertrend line and the MACD line crosses below its signal.

## Details

- **Indicators**: Supertrend (ATR 10, factor 3), MACD (12, 26, 9)
- **Entry**: Price above Supertrend and MACD bullish crossover
- **Exit**: Price below Supertrend and MACD bearish crossover
- **Direction**: Long only
- **Timeframe**: Any
