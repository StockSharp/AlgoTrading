# Ta Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategy based on MACD crossover with support and resistance pivots, RSI and ADX confirmation. Two profit targets with partial exit are used.

## Details

- **Entry**
  - **Long**: MACD crosses above signal, price above resistance, RSI > 50, +DI > -DI, ADX > 20.
  - **Short**: MACD crosses below signal, price below support, RSI < 50, -DI > +DI, ADX > 20.
- **Exit**: two take-profit levels and a stop-loss.
