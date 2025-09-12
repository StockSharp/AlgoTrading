# Supertrend - SSL Strategy with Toggle
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines the Supertrend indicator with the SSL channel.
A toggle allows requiring confirmation from both indicators before entering a trade.
If confirmation is enabled, the first indicator signal waits for the second before executing.
Positions are closed when an opposite signal appears from either indicator.

## Details

- **Indicators**: Supertrend (ATR 10, factor 2.4), SSL channel (period 13)
- **Entry**: SSL crossover or Supertrend direction change with optional confirmation
- **Exit**: Opposite signal from SSL or Supertrend
- **Direction**: Long and Short
- **Timeframe**: Any
